package tusklanggo

import (
	"fmt"
	"reflect"
	"strconv"
	"strings"
)

// Config represents a generic TuskLang configuration
// Can be mapped to user-defined Go structs

type Config map[string]interface{}

// UnmarshalTSK parses TuskLang data into a user struct
// (like json.Unmarshal)
func UnmarshalTSK(data map[string]interface{}, v interface{}) error {
	val := reflect.ValueOf(v)
	if val.Kind() != reflect.Ptr {
		return fmt.Errorf("UnmarshalTSK: v must be a pointer")
	}
	
	val = val.Elem()
	if val.Kind() != reflect.Struct {
		return fmt.Errorf("UnmarshalTSK: v must be a pointer to struct")
	}
	
	return unmarshalMap(data, val)
}

// unmarshalMap recursively unmarshals a map into a struct
func unmarshalMap(data map[string]interface{}, val reflect.Value) error {
	typ := val.Type()
	
	for i := 0; i < val.NumField(); i++ {
		field := val.Field(i)
		fieldType := typ.Field(i)
		
		// Get the field name (use json tag if available)
		fieldName := fieldType.Name
		if tag := fieldType.Tag.Get("tsk"); tag != "" {
			fieldName = tag
		} else if tag := fieldType.Tag.Get("json"); tag != "" {
			fieldName = strings.Split(tag, ",")[0]
		}
		
		// Convert to lowercase for case-insensitive matching
		fieldNameLower := strings.ToLower(fieldName)
		
		// Find matching key in data (case-insensitive)
		var value interface{}
		var found bool
		for key, val := range data {
			if strings.ToLower(key) == fieldNameLower {
				value = val
				found = true
				break
			}
		}
		
		if !found {
			continue // Skip if not found
		}
		
		// Set the field value
		if err := setFieldValue(field, value); err != nil {
			return fmt.Errorf("field %s: %w", fieldName, err)
		}
	}
	
	return nil
}

// setFieldValue sets a field value based on its type
func setFieldValue(field reflect.Value, value interface{}) error {
	if !field.CanSet() {
		return fmt.Errorf("field is not settable")
	}
	
	switch field.Kind() {
	case reflect.String:
		if str, ok := value.(string); ok {
			field.SetString(str)
		} else {
			field.SetString(fmt.Sprintf("%v", value))
		}
		
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
		switch v := value.(type) {
		case int:
			field.SetInt(int64(v))
		case float64:
			field.SetInt(int64(v))
		case string:
			if i, err := strconv.ParseInt(v, 10, 64); err == nil {
				field.SetInt(i)
			}
		}
		
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64:
		switch v := value.(type) {
		case int:
			field.SetUint(uint64(v))
		case float64:
			field.SetUint(uint64(v))
		case string:
			if i, err := strconv.ParseUint(v, 10, 64); err == nil {
				field.SetUint(i)
			}
		}
		
	case reflect.Float32, reflect.Float64:
		switch v := value.(type) {
		case float64:
			field.SetFloat(v)
		case int:
			field.SetFloat(float64(v))
		case string:
			if f, err := strconv.ParseFloat(v, 64); err == nil {
				field.SetFloat(f)
			}
		}
		
	case reflect.Bool:
		switch v := value.(type) {
		case bool:
			field.SetBool(v)
		case string:
			field.SetBool(strings.ToLower(v) == "true")
		}
		
	case reflect.Slice:
		if slice, ok := value.([]interface{}); ok {
			sliceType := field.Type().Elem()
			newSlice := reflect.MakeSlice(field.Type(), len(slice), len(slice))
			
			for i, item := range slice {
				itemValue := reflect.New(sliceType).Elem()
				if err := setFieldValue(itemValue, item); err != nil {
					return err
				}
				newSlice.Index(i).Set(itemValue)
			}
			
			field.Set(newSlice)
		}
		
	case reflect.Struct:
		if mapValue, ok := value.(map[string]interface{}); ok {
			return unmarshalMap(mapValue, field)
		}
		
	case reflect.Map:
		if mapValue, ok := value.(map[string]interface{}); ok {
			if field.IsNil() {
				field.Set(reflect.MakeMap(field.Type()))
			}
			
			for key, val := range mapValue {
				keyValue := reflect.ValueOf(key)
				valueValue := reflect.New(field.Type().Elem()).Elem()
				
				if err := setFieldValue(valueValue, val); err != nil {
					return err
				}
				
				field.SetMapIndex(keyValue, valueValue)
			}
		}
	}
	
	return nil
} 