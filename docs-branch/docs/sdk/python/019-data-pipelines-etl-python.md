# 🔄 Data Pipelines & ETL - Python

**"We don't bow to any king" - Data Engineering Edition**

TuskLang empowers you to build robust, scalable data pipelines and ETL (Extract, Transform, Load) workflows for analytics, machine learning, and automation.

## 🚀 Data Pipeline Concepts

- **Extract**: Ingest data from multiple sources (databases, APIs, files, streams)
- **Transform**: Clean, enrich, and reshape data (validation, mapping, aggregation)
- **Load**: Store processed data in target systems (databases, data lakes, warehouses)
- **Orchestration**: Schedule, monitor, and manage pipeline execution

## 🏗️ Building ETL Pipelines with TuskLang

### Basic ETL Pipeline

```python
from tsk import TSK
import pandas as pd

# ETL configuration
etl_config = TSK.from_string("""
[etl]
# Data sources
source_db: @env("SOURCE_DB", "sqlite:///source.db")
target_db: @env("TARGET_DB", "sqlite:///target.db")

# Extract step
extract_data_fujsen = '''
def extract_data(query):
    import pandas as pd
    from sqlalchemy import create_engine
    engine = create_engine(source_db)
    df = pd.read_sql(query, engine)
    return df.to_dict(orient='records')
'''

# Transform step
transform_data_fujsen = '''
def transform_data(records):
    # Example: filter, map, and enrich
    transformed = []
    for rec in records:
        if rec['active']:
            rec['full_name'] = f"{rec['first_name']} {rec['last_name']}"
            rec['signup_year'] = int(rec['signup_date'][:4])
            transformed.append(rec)
    return transformed
'''

# Load step
load_data_fujsen = '''
def load_data(records, table):
    import pandas as pd
    from sqlalchemy import create_engine
    engine = create_engine(target_db)
    df = pd.DataFrame(records)
    df.to_sql(table, engine, if_exists='replace', index=False)
    return {'rows_loaded': len(df)}
'''
""")

# Example ETL run
source_query = "SELECT * FROM users"
extracted = etl_config.execute_fujsen('etl', 'extract_data', source_query)
transformed = etl_config.execute_fujsen('etl', 'transform_data', extracted)
result = etl_config.execute_fujsen('etl', 'load_data', transformed, 'users_cleaned')
print(f"ETL complete: {result['rows_loaded']} rows loaded.")
```

## 🧩 Advanced Data Processing

### Batch vs. Streaming
- **Batch**: Process large datasets on a schedule (nightly, hourly)
- **Streaming**: Process data in real-time as it arrives (Kafka, Redis Streams)

### Data Validation & Cleansing

```python
# Data validation configuration
data_validation = TSK.from_string("""
[data_validation]
validate_record_fujsen = '''
def validate_record(record):
    errors = []
    if not record.get('email') or '@' not in record['email']:
        errors.append('Invalid email')
    if record.get('age', 0) < 0:
        errors.append('Negative age')
    if not record.get('signup_date'):
        errors.append('Missing signup_date')
    return {'valid': not errors, 'errors': errors}
'''

cleanse_data_fujsen = '''
def cleanse_data(records):
    cleaned = []
    for rec in records:
        validation = validate_record(rec)
        if validation['valid']:
            cleaned.append(rec)
    return cleaned
'''
""")
```

### Data Enrichment

```python
# Data enrichment configuration
data_enrichment = TSK.from_string("""
[data_enrichment]
enrich_with_geo_fujsen = '''
def enrich_with_geo(records):
    for rec in records:
        # Example: add country based on IP (mocked)
        rec['country'] = 'USA' if rec.get('ip', '').startswith('192.') else 'Unknown'
    return records
'''
""")
```

## 🔄 Orchestrating Pipelines

### Scheduling with Airflow

```python
# airflow_dag.py
from airflow import DAG
from airflow.operators.python import PythonOperator
from datetime import datetime
from tsk import TSK

def run_etl():
    etl_config = TSK.from_file('etl.tsk')
    source_query = "SELECT * FROM users"
    extracted = etl_config.execute_fujsen('etl', 'extract_data', source_query)
    transformed = etl_config.execute_fujsen('etl', 'transform_data', extracted)
    result = etl_config.execute_fujsen('etl', 'load_data', transformed, 'users_cleaned')
    print(f"ETL complete: {result['rows_loaded']} rows loaded.")

dag = DAG('tusk_etl', start_date=datetime(2024, 1, 1), schedule_interval='@daily')

etl_task = PythonOperator(
    task_id='run_etl',
    python_callable=run_etl,
    dag=dag
)
```

### Orchestrating with Prefect

```python
from prefect import task, flow
from tsk import TSK

@task
def extract():
    etl_config = TSK.from_file('etl.tsk')
    return etl_config.execute_fujsen('etl', 'extract_data', "SELECT * FROM users")

@task
def transform(records):
    etl_config = TSK.from_file('etl.tsk')
    return etl_config.execute_fujsen('etl', 'transform_data', records)

@task
def load(records):
    etl_config = TSK.from_file('etl.tsk')
    return etl_config.execute_fujsen('etl', 'load_data', records, 'users_cleaned')

@flow
def tusk_etl_flow():
    data = extract()
    clean = transform(data)
    result = load(clean)
    print(result)

tusk_etl_flow()
```

## 📊 Data Pipeline Monitoring

- Track pipeline runs, failures, and data quality metrics
- Log row counts, error rates, and processing times
- Use TSK to store and query pipeline metadata

## 🎯 Data Pipeline Best Practices

- Validate and cleanse all input data
- Use idempotent ETL steps for re-runs
- Log all pipeline activity and errors
- Parameterize pipeline configuration with TSK
- Monitor pipeline health and data quality
- Document pipeline logic and data contracts

## 🚀 Next Steps

1. **Define your ETL steps in TSK**
2. **Build Python ETL scripts using TSK and pandas**
3. **Add validation, cleansing, and enrichment**
4. **Orchestrate pipelines with Airflow or Prefect**
5. **Monitor and document your data pipelines**

---

**"We don't bow to any king"** - TuskLang empowers you to build robust, scalable data pipelines and ETL workflows. Extract, transform, load, and orchestrate your data with confidence! 