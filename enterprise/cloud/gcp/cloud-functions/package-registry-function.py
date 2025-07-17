"""
TuskLang Package Registry Google Cloud Function
Serverless package management for TuskLang on GCP
"""

import json
import logging
import os
from datetime import datetime, timedelta
from typing import Dict, Any, Optional
import base64
import hashlib

from google.cloud import storage
from google.cloud import firestore
from google.cloud import pubsub_v1
from google.cloud import secretmanager
import functions_framework

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize GCP clients
storage_client = storage.Client()
firestore_client = firestore.Client()
publisher = pubsub_v1.PublisherClient()
secret_client = secretmanager.SecretManagerServiceClient()

# Configuration
BUCKET_NAME = os.environ.get('PACKAGE_BUCKET_NAME', 'tusklang-packages')
COLLECTION_NAME = os.environ.get('PACKAGE_COLLECTION_NAME', 'packages')
TOPIC_NAME = os.environ.get('PUBSUB_TOPIC_NAME', 'package-events')
PROJECT_ID = os.environ.get('GCP_PROJECT_ID')

class PackageRegistryGCP:
    def __init__(self):
        self.bucket = storage_client.bucket(BUCKET_NAME)
        self.collection = firestore_client.collection(COLLECTION_NAME)
        self.topic_path = publisher.topic_path(PROJECT_ID, TOPIC_NAME)
        
    def upload_package(self, request_data: Dict[str, Any]) -> Dict[str, Any]:
        """Handle package upload requests"""
        try:
            # Parse request
            package_name = request_data.get('name')
            package_version = request_data.get('version')
            package_data = request_data.get('data')
            package_metadata = request_data.get('metadata', {})
            
            if not all([package_name, package_version, package_data]):
                return self._error_response(400, "Missing required fields")
            
            # Validate package
            validation_result = self._validate_package(package_name, package_version, package_data)
            if not validation_result['valid']:
                return self._error_response(400, validation_result['message'])
            
            # Generate package ID and blob name
            package_id = f"{package_name}-{package_version}"
            blob_name = f"packages/{package_name}/{package_version}/package.tsk"
            
            # Check if package already exists
            if self._package_exists(package_name, package_version):
                return self._error_response(409, "Package version already exists")
            
            # Upload to Cloud Storage
            blob = self.bucket.blob(blob_name)
            package_bytes = base64.b64decode(package_data)
            
            blob.metadata = {
                'package-name': package_name,
                'package-version': package_version,
                'upload-time': datetime.utcnow().isoformat()
            }
            
            blob.upload_from_string(
                package_bytes,
                content_type='application/octet-stream'
            )
            
            # Store metadata in Firestore
            package_doc = {
                'package_id': package_id,
                'name': package_name,
                'version': package_version,
                'blob_name': blob_name,
                'size': len(package_bytes),
                'metadata': package_metadata,
                'upload_time': datetime.utcnow(),
                'download_count': 0,
                'last_download': None,
                'checksum': hashlib.sha256(package_bytes).hexdigest()
            }
            
            self.collection.document(package_id).set(package_doc)
            
            # Publish event to Pub/Sub
            self._publish_event('package.uploaded', {
                'package_id': package_id,
                'name': package_name,
                'version': package_version,
                'size': len(package_bytes)
            })
            
            return self._success_response({
                'package_id': package_id,
                'download_url': f"https://storage.googleapis.com/{BUCKET_NAME}/{blob_name}",
                'message': 'Package uploaded successfully'
            })
            
        except Exception as e:
            logger.error(f"Upload error: {str(e)}")
            return self._error_response(500, f"Upload failed: {str(e)}")
    
    def download_package(self, package_name: str, package_version: str) -> Dict[str, Any]:
        """Handle package download requests"""
        try:
            if not all([package_name, package_version]):
                return self._error_response(400, "Missing package name or version")
            
            package_id = f"{package_name}-{package_version}"
            
            # Get package metadata from Firestore
            doc_ref = self.collection.document(package_id)
            doc = doc_ref.get()
            
            if not doc.exists:
                return self._error_response(404, "Package not found")
            
            package_data = doc.to_dict()
            
            # Generate signed URL for download
            blob = self.bucket.blob(package_data['blob_name'])
            signed_url = blob.generate_signed_url(
                version="v4",
                expiration=timedelta(hours=1),
                method="GET"
            )
            
            # Update download statistics
            self._update_download_stats(package_id)
            
            # Publish download event
            self._publish_event('package.downloaded', {
                'package_id': package_id,
                'name': package_name,
                'version': package_version
            })
            
            return self._success_response({
                'package_id': package_id,
                'download_url': signed_url,
                'metadata': package_data.get('metadata', {}),
                'size': package_data['size']
            })
            
        except Exception as e:
            logger.error(f"Download error: {str(e)}")
            return self._error_response(500, f"Download failed: {str(e)}")
    
    def search_packages(self, search_term: str = "", limit: int = 20, offset: int = 0) -> Dict[str, Any]:
        """Handle package search requests"""
        try:
            # Build Firestore query
            query = self.collection
            
            if search_term:
                # Firestore doesn't support full-text search, so we'll do a simple contains check
                # In production, you'd use a search service like Algolia or Elasticsearch
                query = query.where('name', '>=', search_term).where('name', '<=', search_term + '\uf8ff')
            
            # Execute query
            docs = query.limit(limit).offset(offset).stream()
            
            # Process results
            packages = []
            for doc in docs:
                data = doc.to_dict()
                packages.append({
                    'package_id': data['package_id'],
                    'name': data['name'],
                    'version': data['version'],
                    'download_count': data.get('download_count', 0),
                    'upload_time': data['upload_time'].isoformat(),
                    'metadata': data.get('metadata', {})
                })
            
            return self._success_response({
                'packages': packages,
                'total': len(packages),
                'next_offset': offset + limit if len(packages) == limit else None
            })
            
        except Exception as e:
            logger.error(f"Search error: {str(e)}")
            return self._error_response(500, f"Search failed: {str(e)}")
    
    def get_package_info(self, package_name: str, package_version: str) -> Dict[str, Any]:
        """Get detailed package information"""
        try:
            if not all([package_name, package_version]):
                return self._error_response(400, "Missing package name or version")
            
            package_id = f"{package_name}-{package_version}"
            
            # Get package metadata from Firestore
            doc_ref = self.collection.document(package_id)
            doc = doc_ref.get()
            
            if not doc.exists:
                return self._error_response(404, "Package not found")
            
            package_data = doc.to_dict()
            download_url = f"https://storage.googleapis.com/{BUCKET_NAME}/{package_data['blob_name']}"
            
            return self._success_response({
                'package_id': package_id,
                'name': package_data['name'],
                'version': package_data['version'],
                'download_url': download_url,
                'size': package_data['size'],
                'download_count': package_data.get('download_count', 0),
                'upload_time': package_data['upload_time'].isoformat(),
                'last_download': package_data.get('last_download', {}).isoformat() if package_data.get('last_download') else None,
                'metadata': package_data.get('metadata', {}),
                'checksum': package_data.get('checksum')
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
            
            if not data:
                return {'valid': False, 'message': 'Package data is empty'}
            
            # Check size limit
            package_bytes = base64.b64decode(data)
            if len(package_bytes) > 50 * 1024 * 1024:  # 50MB limit
                return {'valid': False, 'message': 'Package too large'}
            
            # Check for valid TuskLang syntax (basic check)
            package_text = package_bytes.decode('utf-8', errors='ignore')
            if not package_text.strip().startswith('#') and ':' not in package_text:
                return {'valid': False, 'message': 'Invalid TuskLang format'}
            
            return {'valid': True, 'message': 'Package is valid'}
            
        except Exception as e:
            return {'valid': False, 'message': f'Validation error: {str(e)}'}
    
    def _package_exists(self, name: str, version: str) -> bool:
        """Check if package already exists"""
        try:
            package_id = f"{name}-{version}"
            doc_ref = self.collection.document(package_id)
            doc = doc_ref.get()
            return doc.exists
        except Exception:
            return False
    
    def _update_download_stats(self, package_id: str):
        """Update download statistics"""
        try:
            doc_ref = self.collection.document(package_id)
            doc_ref.update({
                'download_count': firestore.Increment(1),
                'last_download': datetime.utcnow()
            })
        except Exception as e:
            logger.error(f"Failed to update download stats: {str(e)}")
    
    def _publish_event(self, event_type: str, event_data: Dict[str, Any]):
        """Publish event to Pub/Sub"""
        try:
            message = {
                'event_type': event_type,
                'timestamp': datetime.utcnow().isoformat(),
                'data': event_data
            }
            
            future = publisher.publish(
                self.topic_path,
                json.dumps(message).encode('utf-8')
            )
            future.result()  # Wait for the publish to complete
            
        except Exception as e:
            logger.error(f"Failed to publish event: {str(e)}")
    
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
handler = PackageRegistryGCP()

@functions_framework.http
def package_registry(request):
    """Main Cloud Function handler"""
    try:
        # Handle CORS preflight requests
        if request.method == 'OPTIONS':
            return ('', 204, {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Headers': 'Content-Type',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS'
            })
        
        # Route requests based on HTTP method and path
        if request.method == 'POST' and request.path == '/packages':
            request_data = request.get_json()
            return handler.upload_package(request_data)
            
        elif request.method == 'GET' and '/packages/' in request.path and '/download' in request.path:
            # Extract package name and version from path
            path_parts = request.path.split('/')
            if len(path_parts) >= 4:
                package_name = path_parts[2]
                package_version = path_parts[3]
                return handler.download_package(package_name, package_version)
            else:
                return handler._error_response(400, "Invalid path")
                
        elif request.method == 'GET' and request.path == '/packages/search':
            search_term = request.args.get('q', '')
            limit = int(request.args.get('limit', 20))
            offset = int(request.args.get('offset', 0))
            return handler.search_packages(search_term, limit, offset)
            
        elif request.method == 'GET' and '/packages/' in request.path:
            # Extract package name and version from path
            path_parts = request.path.split('/')
            if len(path_parts) >= 4:
                package_name = path_parts[2]
                package_version = path_parts[3]
                return handler.get_package_info(package_name, package_version)
            else:
                return handler._error_response(400, "Invalid path")
        else:
            return handler._error_response(404, "Endpoint not found")
            
    except Exception as e:
        logger.error(f"Cloud Function error: {str(e)}")
        return handler._error_response(500, f"Internal server error: {str(e)}") 