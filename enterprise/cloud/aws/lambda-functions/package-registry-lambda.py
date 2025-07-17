"""
TuskLang Package Registry AWS Lambda Function
Serverless package management for TuskLang
"""

import json
import boto3
import os
import logging
from typing import Dict, Any, Optional
from datetime import datetime, timedelta
import hashlib
import base64

# Configure logging
logger = logging.getLogger()
logger.setLevel(logging.INFO)

# Initialize AWS clients
s3 = boto3.client('s3')
dynamodb = boto3.resource('dynamodb')
cloudfront = boto3.client('cloudfront')
lambda_client = boto3.client('lambda')

# Configuration
BUCKET_NAME = os.environ.get('PACKAGE_BUCKET_NAME')
TABLE_NAME = os.environ.get('PACKAGE_TABLE_NAME')
DISTRIBUTION_ID = os.environ.get('CLOUDFRONT_DISTRIBUTION_ID')
REGION = os.environ.get('AWS_REGION', 'us-east-1')

class PackageRegistryLambda:
    def __init__(self):
        self.table = dynamodb.Table(TABLE_NAME)
        
    def upload_package(self, event: Dict[str, Any]) -> Dict[str, Any]:
        """Handle package upload requests"""
        try:
            # Parse request
            body = json.loads(event.get('body', '{}'))
            package_name = body.get('name')
            package_version = body.get('version')
            package_data = body.get('data')
            package_metadata = body.get('metadata', {})
            
            if not all([package_name, package_version, package_data]):
                return self._error_response(400, "Missing required fields")
            
            # Validate package
            validation_result = self._validate_package(package_name, package_version, package_data)
            if not validation_result['valid']:
                return self._error_response(400, validation_result['message'])
            
            # Generate package ID and key
            package_id = f"{package_name}-{package_version}"
            package_key = f"packages/{package_name}/{package_version}/package.tsk"
            
            # Check if package already exists
            if self._package_exists(package_name, package_version):
                return self._error_response(409, "Package version already exists")
            
            # Upload to S3
            s3_response = s3.put_object(
                Bucket=BUCKET_NAME,
                Key=package_key,
                Body=package_data,
                ContentType='application/octet-stream',
                Metadata={
                    'package-name': package_name,
                    'package-version': package_version,
                    'upload-time': datetime.utcnow().isoformat()
                }
            )
            
            # Store metadata in DynamoDB
            package_record = {
                'package_id': package_id,
                'name': package_name,
                'version': package_version,
                's3_key': package_key,
                's3_etag': s3_response['ETag'],
                'size': len(package_data),
                'metadata': package_metadata,
                'upload_time': datetime.utcnow().isoformat(),
                'download_count': 0,
                'last_download': None
            }
            
            self.table.put_item(Item=package_record)
            
            # Invalidate CloudFront cache
            self._invalidate_cache(f"/packages/{package_name}/{package_version}/*")
            
            return self._success_response({
                'package_id': package_id,
                'download_url': f"https://cdn.tusklang.org/{package_key}",
                'message': 'Package uploaded successfully'
            })
            
        except Exception as e:
            logger.error(f"Upload error: {str(e)}")
            return self._error_response(500, f"Upload failed: {str(e)}")
    
    def download_package(self, event: Dict[str, Any]) -> Dict[str, Any]:
        """Handle package download requests"""
        try:
            # Parse request
            package_name = event.get('pathParameters', {}).get('name')
            package_version = event.get('pathParameters', {}).get('version')
            
            if not all([package_name, package_version]):
                return self._error_response(400, "Missing package name or version")
            
            package_id = f"{package_name}-{package_version}"
            
            # Get package metadata
            response = self.table.get_item(Key={'package_id': package_id})
            if 'Item' not in response:
                return self._error_response(404, "Package not found")
            
            package_record = response['Item']
            
            # Generate presigned URL for download
            presigned_url = s3.generate_presigned_url(
                'get_object',
                Params={
                    'Bucket': BUCKET_NAME,
                    'Key': package_record['s3_key']
                },
                ExpiresIn=3600  # 1 hour
            )
            
            # Update download statistics
            self._update_download_stats(package_id)
            
            return self._success_response({
                'package_id': package_id,
                'download_url': presigned_url,
                'metadata': package_record['metadata'],
                'size': package_record['size']
            })
            
        except Exception as e:
            logger.error(f"Download error: {str(e)}")
            return self._error_response(500, f"Download failed: {str(e)}")
    
    def search_packages(self, event: Dict[str, Any]) -> Dict[str, Any]:
        """Handle package search requests"""
        try:
            # Parse query parameters
            query_params = event.get('queryStringParameters', {}) or {}
            search_term = query_params.get('q', '')
            limit = int(query_params.get('limit', 20))
            offset = int(query_params.get('offset', 0))
            
            # Build scan parameters
            scan_params = {
                'Limit': limit,
                'ExclusiveStartKey': {'package_id': offset} if offset > 0 else None
            }
            
            # Add filter if search term provided
            if search_term:
                scan_params['FilterExpression'] = 'contains(#name, :search) OR contains(#version, :search)'
                scan_params['ExpressionAttributeNames'] = {
                    '#name': 'name',
                    '#version': 'version'
                }
                scan_params['ExpressionAttributeValues'] = {
                    ':search': search_term
                }
            
            # Scan DynamoDB
            response = self.table.scan(**scan_params)
            packages = response.get('Items', [])
            
            # Format results
            results = []
            for package in packages:
                results.append({
                    'package_id': package['package_id'],
                    'name': package['name'],
                    'version': package['version'],
                    'download_count': package.get('download_count', 0),
                    'upload_time': package['upload_time'],
                    'metadata': package.get('metadata', {})
                })
            
            return self._success_response({
                'packages': results,
                'total': len(results),
                'next_offset': offset + limit if len(results) == limit else None
            })
            
        except Exception as e:
            logger.error(f"Search error: {str(e)}")
            return self._error_response(500, f"Search failed: {str(e)}")
    
    def get_package_info(self, event: Dict[str, Any]) -> Dict[str, Any]:
        """Get detailed package information"""
        try:
            package_name = event.get('pathParameters', {}).get('name')
            package_version = event.get('pathParameters', {}).get('version')
            
            if not all([package_name, package_version]):
                return self._error_response(400, "Missing package name or version")
            
            package_id = f"{package_name}-{package_version}"
            
            # Get package metadata
            response = self.table.get_item(Key={'package_id': package_id})
            if 'Item' not in response:
                return self._error_response(404, "Package not found")
            
            package_record = response['Item']
            
            # Get download URL
            download_url = f"https://cdn.tusklang.org/{package_record['s3_key']}"
            
            return self._success_response({
                'package_id': package_id,
                'name': package_record['name'],
                'version': package_record['version'],
                'download_url': download_url,
                'size': package_record['size'],
                'download_count': package_record.get('download_count', 0),
                'upload_time': package_record['upload_time'],
                'last_download': package_record.get('last_download'),
                'metadata': package_record.get('metadata', {})
            })
            
        except Exception as e:
            logger.error(f"Package info error: {str(e)}")
            return self._error_response(500, f"Failed to get package info: {str(e)}")
    
    def _validate_package(self, name: str, version: str, data: str) -> Dict[str, Any]:
        """Validate package data"""
        try:
            # Basic validation
            if not name or len(name) > 100:
                return {'valid': False, 'message': 'Invalid package name'}
            
            if not version or len(version) > 50:
                return {'valid': False, 'message': 'Invalid package version'}
            
            if not data or len(data) > 50 * 1024 * 1024:  # 50MB limit
                return {'valid': False, 'message': 'Package too large or empty'}
            
            # Check for valid TuskLang syntax (basic check)
            if not data.strip().startswith('#') and ':' not in data:
                return {'valid': False, 'message': 'Invalid TuskLang format'}
            
            return {'valid': True, 'message': 'Package is valid'}
            
        except Exception as e:
            return {'valid': False, 'message': f'Validation error: {str(e)}'}
    
    def _package_exists(self, name: str, version: str) -> bool:
        """Check if package already exists"""
        try:
            package_id = f"{name}-{version}"
            response = self.table.get_item(Key={'package_id': package_id})
            return 'Item' in response
        except Exception:
            return False
    
    def _update_download_stats(self, package_id: str):
        """Update download statistics"""
        try:
            self.table.update_item(
                Key={'package_id': package_id},
                UpdateExpression='SET download_count = download_count + :inc, last_download = :time',
                ExpressionAttributeValues={
                    ':inc': 1,
                    ':time': datetime.utcnow().isoformat()
                }
            )
        except Exception as e:
            logger.error(f"Failed to update download stats: {str(e)}")
    
    def _invalidate_cache(self, path: str):
        """Invalidate CloudFront cache"""
        try:
            cloudfront.create_invalidation(
                DistributionId=DISTRIBUTION_ID,
                InvalidationBatch={
                    'Paths': {
                        'Quantity': 1,
                        'Items': [path]
                    },
                    'CallerReference': f"lambda-{datetime.utcnow().timestamp()}"
                }
            )
        except Exception as e:
            logger.error(f"Failed to invalidate cache: {str(e)}")
    
    def _success_response(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """Generate success response"""
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS'
            },
            'body': json.dumps(data)
        }
    
    def _error_response(self, status_code: int, message: str) -> Dict[str, Any]:
        """Generate error response"""
        return {
            'statusCode': status_code,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS'
            },
            'body': json.dumps({
                'error': message,
                'status_code': status_code
            })
        }

# Initialize handler
handler = PackageRegistryLambda()

def lambda_handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """Main Lambda handler"""
    try:
        # Route requests based on HTTP method and path
        http_method = event.get('httpMethod', 'GET')
        path = event.get('path', '')
        
        if http_method == 'POST' and path == '/packages':
            return handler.upload_package(event)
        elif http_method == 'GET' and '/packages/' in path and '/download' in path:
            return handler.download_package(event)
        elif http_method == 'GET' and path == '/packages/search':
            return handler.search_packages(event)
        elif http_method == 'GET' and '/packages/' in path:
            return handler.get_package_info(event)
        else:
            return handler._error_response(404, "Endpoint not found")
            
    except Exception as e:
        logger.error(f"Lambda handler error: {str(e)}")
        return handler._error_response(500, f"Internal server error: {str(e)}") 