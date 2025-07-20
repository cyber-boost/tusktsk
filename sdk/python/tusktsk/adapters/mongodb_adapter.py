#!/usr/bin/env python3
"""
MongoDB Adapter for TuskLang Python SDK
=======================================
Implements @mongodb operator with full CRUD operations
"""

import pymongo
from pymongo import MongoClient
from typing import Any, Dict, List, Optional, Union
import json
import logging
from datetime import datetime
import re

logger = logging.getLogger(__name__)


class MongoDBAdapter:
    """MongoDB adapter for TuskLang @mongodb operator"""
    
    def __init__(self):
        self.client = None
        self.db = None
        self.collection = None
        self.connection_string = None
        self.database_name = None
        self.collection_name = None
    
    def connect(self, connection_string: str, database_name: str, collection_name: str = None) -> bool:
        """Connect to MongoDB"""
        try:
            self.connection_string = connection_string
            self.database_name = database_name
            self.collection_name = collection_name
            
            self.client = MongoClient(connection_string)
            self.db = self.client[database_name]
            
            if collection_name:
                self.collection = self.db[collection_name]
            
            # Test connection
            self.client.admin.command('ping')
            logger.info(f"Connected to MongoDB: {database_name}")
            return True
            
        except Exception as e:
            logger.error(f"MongoDB connection error: {str(e)}")
            return False
    
    def execute_mongodb(self, operation: str, params: Dict[str, Any]) -> Any:
        """Execute MongoDB operation"""
        try:
            if operation == "find":
                return self._execute_find(params)
            elif operation == "findOne":
                return self._execute_find_one(params)
            elif operation == "insert":
                return self._execute_insert(params)
            elif operation == "update":
                return self._execute_update(params)
            elif operation == "delete":
                return self._execute_delete(params)
            elif operation == "aggregate":
                return self._execute_aggregate(params)
            elif operation == "count":
                return self._execute_count(params)
            elif operation == "distinct":
                return self._execute_distinct(params)
            else:
                return f"Unknown MongoDB operation: {operation}"
                
        except Exception as e:
            logger.error(f"MongoDB operation error: {str(e)}")
            return f"MongoDB Error: {str(e)}"
    
    def _execute_find(self, params: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Execute find operation"""
        try:
            query = params.get("query", {})
            projection = params.get("projection", {})
            sort = params.get("sort", None)
            limit = params.get("limit", 0)
            skip = params.get("skip", 0)
            
            cursor = self.collection.find(query, projection)
            
            if sort:
                cursor = cursor.sort(sort)
            if skip > 0:
                cursor = cursor.skip(skip)
            if limit > 0:
                cursor = cursor.limit(limit)
            
            results = list(cursor)
            
            # Convert ObjectId to string for JSON serialization
            for doc in results:
                if '_id' in doc:
                    doc['_id'] = str(doc['_id'])
            
            return results
            
        except Exception as e:
            logger.error(f"MongoDB find error: {str(e)}")
            return []
    
    def _execute_find_one(self, params: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Execute findOne operation"""
        try:
            query = params.get("query", {})
            projection = params.get("projection", {})
            
            result = self.collection.find_one(query, projection)
            
            if result and '_id' in result:
                result['_id'] = str(result['_id'])
            
            return result
            
        except Exception as e:
            logger.error(f"MongoDB findOne error: {str(e)}")
            return None
    
    def _execute_insert(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Execute insert operation"""
        try:
            document = params.get("document", {})
            many = params.get("many", False)
            
            if many:
                documents = params.get("documents", [])
                if not documents:
                    documents = [document]
                
                result = self.collection.insert_many(documents)
                return {
                    "inserted_ids": [str(id) for id in result.inserted_ids],
                    "acknowledged": result.acknowledged
                }
            else:
                result = self.collection.insert_one(document)
                return {
                    "inserted_id": str(result.inserted_id),
                    "acknowledged": result.acknowledged
                }
                
        except Exception as e:
            logger.error(f"MongoDB insert error: {str(e)}")
            return {"error": str(e)}
    
    def _execute_update(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Execute update operation"""
        try:
            filter_query = params.get("filter", {})
            update_data = params.get("update", {})
            many = params.get("many", False)
            upsert = params.get("upsert", False)
            
            if many:
                result = self.collection.update_many(
                    filter_query, 
                    update_data, 
                    upsert=upsert
                )
            else:
                result = self.collection.update_one(
                    filter_query, 
                    update_data, 
                    upsert=upsert
                )
            
            return {
                "matched_count": result.matched_count,
                "modified_count": result.modified_count,
                "upserted_id": str(result.upserted_id) if result.upserted_id else None,
                "acknowledged": result.acknowledged
            }
            
        except Exception as e:
            logger.error(f"MongoDB update error: {str(e)}")
            return {"error": str(e)}
    
    def _execute_delete(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Execute delete operation"""
        try:
            filter_query = params.get("filter", {})
            many = params.get("many", False)
            
            if many:
                result = self.collection.delete_many(filter_query)
            else:
                result = self.collection.delete_one(filter_query)
            
            return {
                "deleted_count": result.deleted_count,
                "acknowledged": result.acknowledged
            }
            
        except Exception as e:
            logger.error(f"MongoDB delete error: {str(e)}")
            return {"error": str(e)}
    
    def _execute_aggregate(self, params: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Execute aggregate operation"""
        try:
            pipeline = params.get("pipeline", [])
            
            cursor = self.collection.aggregate(pipeline)
            results = list(cursor)
            
            # Convert ObjectId to string for JSON serialization
            for doc in results:
                if '_id' in doc:
                    doc['_id'] = str(doc['_id'])
            
            return results
            
        except Exception as e:
            logger.error(f"MongoDB aggregate error: {str(e)}")
            return []
    
    def _execute_count(self, params: Dict[str, Any]) -> int:
        """Execute count operation"""
        try:
            query = params.get("query", {})
            return self.collection.count_documents(query)
            
        except Exception as e:
            logger.error(f"MongoDB count error: {str(e)}")
            return 0
    
    def _execute_distinct(self, params: Dict[str, Any]) -> List[Any]:
        """Execute distinct operation"""
        try:
            field = params.get("field", "")
            query = params.get("query", {})
            
            return self.collection.distinct(field, query)
            
        except Exception as e:
            logger.error(f"MongoDB distinct error: {str(e)}")
            return []
    
    def close(self):
        """Close MongoDB connection"""
        if self.client:
            self.client.close()
            logger.info("MongoDB connection closed")


# Global adapter instance
_mongodb_adapter = MongoDBAdapter()


def execute_mongodb(params: str) -> Any:
    """Execute @mongodb operator"""
    try:
        # Parse parameters: operation(params)
        # Example: @mongodb("find", {"query": {"status": "active"}})
        mongodb_match = re.match(r'^["\']([^"\']*)["\'](?:,\s*(.+))?$', params)
        if mongodb_match:
            operation = mongodb_match.group(1)
            operation_params = mongodb_match.group(2) if mongodb_match.group(2) else "{}"
            
            # Parse operation parameters
            try:
                params_dict = json.loads(operation_params)
            except:
                params_dict = {}
            
            # Check if we need to connect
            if "connection" in params_dict:
                connection_info = params_dict["connection"]
                connection_string = connection_info.get("uri", "mongodb://localhost:27017")
                database_name = connection_info.get("database", "test")
                collection_name = connection_info.get("collection", None)
                
                if not _mongodb_adapter.connect(connection_string, database_name, collection_name):
                    return "MongoDB connection failed"
            
            # Execute operation
            return _mongodb_adapter.execute_mongodb(operation, params_dict)
        
        return "Invalid MongoDB parameters"
        
    except Exception as e:
        logger.error(f"MongoDB operator error: {str(e)}")
        return f"@mongodb({params}) - Error: {str(e)}"


# Convenience functions for direct use
def mongodb_find(query: Dict[str, Any], projection: Dict[str, Any] = None, 
                sort: List[tuple] = None, limit: int = 0, skip: int = 0) -> List[Dict[str, Any]]:
    """Find documents in MongoDB"""
    params = {"query": query}
    if projection:
        params["projection"] = projection
    if sort:
        params["sort"] = sort
    if limit > 0:
        params["limit"] = limit
    if skip > 0:
        params["skip"] = skip
    
    return _mongodb_adapter._execute_find(params)


def mongodb_insert(document: Dict[str, Any], collection_name: str = None) -> Dict[str, Any]:
    """Insert document into MongoDB"""
    if collection_name and _mongodb_adapter.collection_name != collection_name:
        _mongodb_adapter.collection = _mongodb_adapter.db[collection_name]
    
    params = {"document": document}
    return _mongodb_adapter._execute_insert(params)


def mongodb_update(filter_query: Dict[str, Any], update_data: Dict[str, Any], 
                  many: bool = False, upsert: bool = False) -> Dict[str, Any]:
    """Update documents in MongoDB"""
    params = {
        "filter": filter_query,
        "update": update_data,
        "many": many,
        "upsert": upsert
    }
    return _mongodb_adapter._execute_update(params)


def mongodb_delete(filter_query: Dict[str, Any], many: bool = False) -> Dict[str, Any]:
    """Delete documents from MongoDB"""
    params = {
        "filter": filter_query,
        "many": many
    }
    return _mongodb_adapter._execute_delete(params)


def mongodb_aggregate(pipeline: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    """Execute aggregation pipeline in MongoDB"""
    params = {"pipeline": pipeline}
    return _mongodb_adapter._execute_aggregate(params)


def mongodb_count(query: Dict[str, Any] = None) -> int:
    """Count documents in MongoDB"""
    params = {"query": query or {}}
    return _mongodb_adapter._execute_count(params)


def mongodb_distinct(field: str, query: Dict[str, Any] = None) -> List[Any]:
    """Get distinct values from MongoDB"""
    params = {
        "field": field,
        "query": query or {}
    }
    return _mongodb_adapter._execute_distinct(params)