# Search Integration with TuskLang and Ruby

This guide covers integrating search engines with TuskLang and Ruby applications for powerful full-text search capabilities.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Index Management](#index-management)
5. [Search Implementation](#search-implementation)
6. [Advanced Features](#advanced-features)
7. [Performance Optimization](#performance-optimization)
8. [Testing](#testing)
9. [Deployment](#deployment)

## Overview

Search integration provides powerful full-text search capabilities for applications. This guide shows how to integrate various search engines with TuskLang and Ruby applications.

### Key Features

- **Multiple search backends** (Elasticsearch, Solr, PostgreSQL Full-Text Search)
- **Advanced querying** with filters and aggregations
- **Real-time indexing** and updates
- **Search suggestions** and autocomplete
- **Faceted search** and filtering
- **Search analytics** and monitoring

## Installation

### Dependencies

```ruby
# Gemfile
gem 'elasticsearch'
gem 'searchkick'
gem 'pg_search'
gem 'sunspot_solr'
gem 'redis'
gem 'connection_pool'
```

### TuskLang Configuration

```tusk
# config/search.tusk
search:
  backend: "elasticsearch"  # elasticsearch, solr, postgresql
  
  elasticsearch:
    url: "http://localhost:9200"
    index_prefix: "tusk_"
    number_of_shards: 1
    number_of_replicas: 0
    refresh_interval: "1s"
    bulk_size: 1000
    timeout: 30
  
  solr:
    url: "http://localhost:8983/solr"
    core: "tusk_core"
    timeout: 30
    batch_size: 1000
  
  postgresql:
    dictionary: "english"
    trigram_similarity_threshold: 0.3
    full_text_search_enabled: true
  
  indexing:
    auto_index: true
    batch_size: 100
    background_jobs: true
    real_time_updates: true
  
  search:
    default_operator: "AND"
    fuzzy_matching: true
    highlight_enabled: true
    suggest_enabled: true
    max_results: 1000
  
  monitoring:
    enabled: true
    metrics_port: 9090
    health_check_interval: 30
```

## Basic Setup

### Search Manager

```ruby
# app/search/search_manager.rb
class SearchManager
  include Singleton

  def initialize
    @config = Rails.application.config.search
    @backend = create_backend
  end

  def index(model_class, records)
    @backend.index(model_class, records)
  end

  def search(query, options = {})
    @backend.search(query, options)
  end

  def suggest(query, options = {})
    @backend.suggest(query, options)
  end

  def delete_index(index_name)
    @backend.delete_index(index_name)
  end

  def reindex(model_class)
    @backend.reindex(model_class)
  end

  def health_check
    @backend.health_check
  end

  private

  def create_backend
    case @config[:backend]
    when 'elasticsearch'
      ElasticsearchBackend.new(@config[:elasticsearch])
    when 'solr'
      SolrBackend.new(@config[:solr])
    when 'postgresql'
      PostgreSQLBackend.new(@config[:postgresql])
    else
      raise "Unsupported search backend: #{@config[:backend]}"
    end
  end
end
```

### Base Searchable

```ruby
# app/search/base_searchable.rb
module BaseSearchable
  extend ActiveSupport::Concern

  included do
    after_create :index_for_search
    after_update :update_search_index
    after_destroy :remove_from_search_index
  end

  def index_for_search
    return unless searchable?
    
    SearchManager.instance.index(self.class, [self])
  end

  def update_search_index
    return unless searchable?
    
    SearchManager.instance.index(self.class, [self])
  end

  def remove_from_search_index
    SearchManager.instance.delete_document(self.class, id)
  end

  def searchable?
    true
  end

  def search_data
    raise NotImplementedError, "#{self.class} must implement search_data"
  end

  def search_suggestions
    raise NotImplementedError, "#{self.class} must implement search_suggestions"
  end
end
```

## Index Management

### Elasticsearch Backend

```ruby
# app/search/backends/elasticsearch_backend.rb
class ElasticsearchBackend
  def initialize(config)
    @config = config
    @client = Elasticsearch::Client.new(url: config[:url])
    @index_prefix = config[:index_prefix]
  end

  def index(model_class, records)
    index_name = index_name_for(model_class)
    ensure_index_exists(index_name, model_class)
    
    bulk_data = records.map do |record|
      {
        index: {
          _index: index_name,
          _id: record.id,
          _type: '_doc',
          data: record.search_data
        }
      }
    end
    
    @client.bulk(body: bulk_data) if bulk_data.any?
  end

  def search(query, options = {})
    index_name = options[:index] || @index_prefix + '*'
    
    search_params = build_search_params(query, options)
    response = @client.search(index: index_name, body: search_params)
    
    SearchResult.new(response, options[:model_class])
  end

  def suggest(query, options = {})
    index_name = options[:index] || @index_prefix + '*'
    
    suggest_params = {
      index: index_name,
      body: {
        suggest: {
          suggestions: {
            prefix: query,
            completion: {
              field: 'suggest',
              size: options[:size] || 10
            }
          }
        }
      }
    }
    
    response = @client.search(suggest_params)
    response['suggest']['suggestions'].first['options'].map { |option| option['text'] }
  end

  def delete_index(index_name)
    full_index_name = "#{@index_prefix}#{index_name}"
    @client.indices.delete(index: full_index_name) if @client.indices.exists(index: full_index_name)
  end

  def reindex(model_class)
    index_name = index_name_for(model_class)
    temp_index_name = "#{index_name}_temp"
    
    # Create temporary index
    create_index(temp_index_name, model_class)
    
    # Reindex all records
    model_class.find_in_batches(batch_size: @config[:batch_size]) do |batch|
      index(model_class, batch)
    end
    
    # Swap indices
    @client.indices.delete(index: index_name) if @client.indices.exists(index: index_name)
    @client.indices.put_alias(index: temp_index_name, name: index_name)
  end

  def health_check
    begin
      response = @client.cluster.health
      {
        status: response['status'],
        number_of_nodes: response['number_of_nodes'],
        active_shards: response['active_shards']
      }
    rescue => e
      { status: 'error', error: e.message }
    end
  end

  private

  def index_name_for(model_class)
    "#{@index_prefix}#{model_class.name.underscore}"
  end

  def ensure_index_exists(index_name, model_class)
    return if @client.indices.exists(index: index_name)
    create_index(index_name, model_class)
  end

  def create_index(index_name, model_class)
    mapping = build_mapping(model_class)
    
    @client.indices.create(
      index: index_name,
      body: {
        settings: {
          number_of_shards: @config[:number_of_shards],
          number_of_replicas: @config[:number_of_replicas],
          refresh_interval: @config[:refresh_interval]
        },
        mappings: {
          properties: mapping
        }
      }
    )
  end

  def build_mapping(model_class)
    # This would be customized based on the model's search_data method
    {
      id: { type: 'integer' },
      created_at: { type: 'date' },
      updated_at: { type: 'date' },
      content: {
        type: 'text',
        analyzer: 'standard',
        fields: {
          keyword: { type: 'keyword' },
          suggest: { type: 'completion' }
        }
      }
    }
  end

  def build_search_params(query, options)
    {
      query: build_query(query, options),
      highlight: build_highlight(options),
      aggs: build_aggregations(options),
      sort: build_sort(options),
      from: options[:from] || 0,
      size: options[:size] || @config[:search][:max_results]
    }
  end

  def build_query(query, options)
    if query.is_a?(String)
      {
        multi_match: {
          query: query,
          fields: options[:fields] || ['content^2', 'title'],
          operator: @config[:search][:default_operator],
          fuzziness: @config[:search][:fuzzy_matching] ? 'AUTO' : nil
        }
      }
    else
      query
    end
  end

  def build_highlight(options)
    return {} unless @config[:search][:highlight_enabled]
    
    {
      fields: {
        content: {},
        title: {}
      }
    }
  end

  def build_aggregations(options)
    return {} unless options[:aggs]
    
    options[:aggs].each_with_object({}) do |(name, config), aggs|
      aggs[name] = {
        terms: {
          field: config[:field],
          size: config[:size] || 10
        }
      }
    end
  end

  def build_sort(options)
    return [] unless options[:sort]
    
    options[:sort].map do |field, direction|
      { field => { order: direction } }
    end
  end
end
```

### Solr Backend

```ruby
# app/search/backends/solr_backend.rb
class SolrBackend
  def initialize(config)
    @config = config
    @client = RSolr.connect(url: config[:url])
  end

  def index(model_class, records)
    documents = records.map { |record| build_document(record) }
    
    @client.add(documents)
    @client.commit
  end

  def search(query, options = {})
    params = build_search_params(query, options)
    response = @client.get('select', params: params)
    
    SearchResult.new(response, options[:model_class])
  end

  def suggest(query, options = {})
    params = {
      q: query,
      'suggest.dictionary': 'default',
      'suggest.count': options[:size] || 10
    }
    
    response = @client.get('suggest', params: params)
    response['suggest']['default'][query]['suggestions'].map { |s| s['term'] }
  end

  def delete_index(index_name)
    @client.delete_by_query("*:*")
    @client.commit
  end

  def reindex(model_class)
    # Clear existing data
    delete_index(model_class.name)
    
    # Reindex all records
    model_class.find_in_batches(batch_size: @config[:batch_size]) do |batch|
      index(model_class, batch)
    end
  end

  def health_check
    begin
      response = @client.get('admin/ping')
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end

  private

  def build_document(record)
    {
      id: record.id,
      type: record.class.name,
      created_at: record.created_at,
      updated_at: record.updated_at,
      **record.search_data
    }
  end

  def build_search_params(query, options)
    {
      q: query,
      start: options[:from] || 0,
      rows: options[:size] || @config[:search][:max_results],
      hl: @config[:search][:highlight_enabled],
      'hl.fl': 'content,title',
      sort: build_sort(options),
      fq: build_filters(options)
    }
  end

  def build_sort(options)
    return nil unless options[:sort]
    
    options[:sort].map { |field, direction| "#{field} #{direction}" }.join(',')
  end

  def build_filters(options)
    return [] unless options[:filters]
    
    options[:filters].map { |field, value| "#{field}:#{value}" }
  end
end
```

### PostgreSQL Backend

```ruby
# app/search/backends/postgresql_backend.rb
class PostgreSQLBackend
  def initialize(config)
    @config = config
  end

  def index(model_class, records)
    # PostgreSQL full-text search is typically handled through triggers
    # This method would update the search vectors
    records.each do |record|
      update_search_vector(record)
    end
  end

  def search(query, options = {})
    sql = build_search_sql(query, options)
    results = ActiveRecord::Base.connection.execute(sql)
    
    SearchResult.new(results, options[:model_class])
  end

  def suggest(query, options = {})
    sql = build_suggest_sql(query, options)
    results = ActiveRecord::Base.connection.execute(sql)
    
    results.map { |row| row['suggestion'] }
  end

  def delete_index(index_name)
    # For PostgreSQL, this would drop the search index
    ActiveRecord::Base.connection.execute("DROP INDEX IF EXISTS #{index_name}_search_idx")
  end

  def reindex(model_class)
    # Rebuild search vectors for all records
    model_class.find_in_batches(batch_size: @config[:batch_size]) do |batch|
      batch.each { |record| update_search_vector(record) }
    end
    
    # Rebuild search index
    rebuild_search_index(model_class)
  end

  def health_check
    begin
      ActiveRecord::Base.connection.execute("SELECT 1")
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end

  private

  def update_search_vector(record)
    search_data = record.search_data
    search_vector = build_search_vector(search_data)
    
    record.class.where(id: record.id).update_all(
      search_vector: search_vector,
      updated_at: Time.current
    )
  end

  def build_search_vector(search_data)
    # Convert search data to PostgreSQL tsvector
    content = search_data.values.compact.join(' ')
    ActiveRecord::Base.connection.execute(
      "SELECT to_tsvector('#{@config[:dictionary]}', #{ActiveRecord::Base.connection.quote(content)})"
    ).first['to_tsvector']
  end

  def build_search_sql(query, options)
    tsquery = build_tsquery(query)
    model_class = options[:model_class]
    
    sql = "SELECT *, ts_rank(search_vector, #{tsquery}) as rank"
    sql += " FROM #{model_class.table_name}"
    sql += " WHERE search_vector @@ #{tsquery}"
    sql += build_filters_sql(options[:filters]) if options[:filters]
    sql += " ORDER BY rank DESC"
    sql += " LIMIT #{options[:size] || @config[:search][:max_results]}"
    sql += " OFFSET #{options[:from] || 0}"
    
    sql
  end

  def build_tsquery(query)
    # Convert query to PostgreSQL tsquery
    ActiveRecord::Base.connection.execute(
      "SELECT to_tsquery('#{@config[:dictionary]}', #{ActiveRecord::Base.connection.quote(query)})"
    ).first['to_tsquery']
  end

  def build_suggest_sql(query, options)
    model_class = options[:model_class]
    
    "SELECT DISTINCT suggestion FROM (
      SELECT unnest(string_to_array(content, ' ')) as suggestion
      FROM #{model_class.table_name}
      WHERE content ILIKE #{ActiveRecord::Base.connection.quote("%#{query}%")}
    ) suggestions
    WHERE similarity(suggestion, #{ActiveRecord::Base.connection.quote(query)}) > #{@config[:trigram_similarity_threshold]}
    ORDER BY similarity(suggestion, #{ActiveRecord::Base.connection.quote(query)}) DESC
    LIMIT #{options[:size] || 10}"
  end

  def build_filters_sql(filters)
    return '' unless filters
    
    conditions = filters.map do |field, value|
      "#{field} = #{ActiveRecord::Base.connection.quote(value)}"
    end
    
    " AND #{conditions.join(' AND ')}"
  end

  def rebuild_search_index(model_class)
    # Create or replace search index
    index_name = "#{model_class.table_name}_search_idx"
    
    ActiveRecord::Base.connection.execute("DROP INDEX IF EXISTS #{index_name}")
    ActiveRecord::Base.connection.execute(
      "CREATE INDEX #{index_name} ON #{model_class.table_name} USING gin(search_vector)"
    )
  end
end
```

## Search Implementation

### Search Result

```ruby
# app/search/search_result.rb
class SearchResult
  attr_reader :total_count, :results, :aggregations, :highlights

  def initialize(response, model_class = nil)
    @response = response
    @model_class = model_class
    parse_response
  end

  def results
    @results ||= []
  end

  def total_count
    @total_count ||= 0
  end

  def aggregations
    @aggregations ||= {}
  end

  def highlights
    @highlights ||= {}
  end

  def empty?
    results.empty?
  end

  def any?
    results.any?
  end

  def size
    results.size
  end

  private

  def parse_response
    case @response
    when Hash
      parse_elasticsearch_response
    when Array
      parse_postgresql_response
    else
      parse_solr_response
    end
  end

  def parse_elasticsearch_response
    hits = @response['hits']
    @total_count = hits['total']['value']
    @results = hits['hits'].map { |hit| parse_elasticsearch_hit(hit) }
    @aggregations = @response['aggregations'] || {}
    @highlights = parse_highlights(@response['hits']['hits'])
  end

  def parse_elasticsearch_hit(hit)
    {
      id: hit['_id'],
      score: hit['_score'],
      source: hit['_source'],
      highlights: hit['highlight']
    }
  end

  def parse_solr_response
    response = @response['response']
    @total_count = response['numFound']
    @results = response['docs'].map { |doc| parse_solr_doc(doc) }
    @aggregations = parse_solr_facets(@response['facet_counts'])
    @highlights = parse_solr_highlights(@response['highlighting'])
  end

  def parse_solr_doc(doc)
    {
      id: doc['id'],
      score: doc['score'],
      source: doc
    }
  end

  def parse_postgresql_response
    @total_count = @response.count
    @results = @response.map { |row| parse_postgresql_row(row) }
  end

  def parse_postgresql_row(row)
    {
      id: row['id'],
      score: row['rank'],
      source: row
    }
  end

  def parse_highlights(hits)
    hits.each_with_object({}) do |hit, highlights|
      highlights[hit['_id']] = hit['highlight'] if hit['highlight']
    end
  end

  def parse_solr_facets(facet_counts)
    return {} unless facet_counts
    
    facet_counts['facet_fields'].each_with_object({}) do |(field, values), facets|
      facets[field] = values.each_slice(2).map { |term, count| { term: term, count: count } }
    end
  end

  def parse_solr_highlights(highlighting)
    highlighting || {}
  end
end
```

### Search Service

```ruby
# app/search/search_service.rb
class SearchService
  include Singleton

  def initialize
    @search_manager = SearchManager.instance
    @config = Rails.application.config.search
  end

  def search_users(query, options = {})
    search(User, query, options.merge(index: 'users'))
  end

  def search_posts(query, options = {})
    search(Post, query, options.merge(index: 'posts'))
  end

  def search_all(query, options = {})
    search(nil, query, options)
  end

  def suggest_users(query, options = {})
    suggest(User, query, options.merge(index: 'users'))
  end

  def suggest_posts(query, options = {})
    suggest(Post, query, options.merge(index: 'posts'))
  end

  def index_user(user)
    index(User, [user])
  end

  def index_post(post)
    index(Post, [post])
  end

  def reindex_users
    reindex(User)
  end

  def reindex_posts
    reindex(Post)
  end

  private

  def search(model_class, query, options = {})
    Rails.logger.info "Searching #{model_class&.name || 'all'} for: #{query}"
    
    start_time = Time.current
    result = @search_manager.search(query, options.merge(model_class: model_class))
    duration = Time.current - start_time
    
    track_search_metrics(query, result.total_count, duration)
    
    result
  rescue => e
    Rails.logger.error "Search error: #{e.message}"
    track_search_error(query, e.message)
    SearchResult.new({})
  end

  def suggest(model_class, query, options = {})
    Rails.logger.info "Suggesting #{model_class&.name || 'all'} for: #{query}"
    
    @search_manager.suggest(query, options.merge(model_class: model_class))
  rescue => e
    Rails.logger.error "Suggest error: #{e.message}"
    []
  end

  def index(model_class, records)
    Rails.logger.info "Indexing #{records.size} #{model_class.name} records"
    
    @search_manager.index(model_class, records)
  rescue => e
    Rails.logger.error "Indexing error: #{e.message}"
    raise e
  end

  def reindex(model_class)
    Rails.logger.info "Reindexing #{model_class.name}"
    
    @search_manager.reindex(model_class)
  rescue => e
    Rails.logger.error "Reindexing error: #{e.message}"
    raise e
  end

  def track_search_metrics(query, total_count, duration)
    return unless @config[:monitoring][:enabled]
    
    # Implementation would send metrics to monitoring system
    Rails.logger.debug "Search metric: #{query} - #{total_count} results - #{duration * 1000}ms"
  end

  def track_search_error(query, error)
    return unless @config[:monitoring][:enabled]
    
    # Implementation would send error metrics to monitoring system
    Rails.logger.debug "Search error metric: #{query} - #{error}"
  end
end
```

## Advanced Features

### Faceted Search

```ruby
# app/search/faceted_search.rb
class FacetedSearch
  def initialize(base_query, facets = {})
    @base_query = base_query
    @facets = facets
  end

  def search(model_class, options = {})
    search_options = build_search_options(options)
    result = SearchService.instance.search(model_class, @base_query, search_options)
    
    FacetedSearchResult.new(result, @facets)
  end

  private

  def build_search_options(options)
    {
      aggs: build_aggregations,
      filters: build_filters(options[:filters]),
      sort: options[:sort],
      from: options[:from],
      size: options[:size]
    }
  end

  def build_aggregations
    @facets.each_with_object({}) do |(field, config), aggs|
      aggs[field] = {
        field: config[:field],
        size: config[:size] || 10
      }
    end
  end

  def build_filters(selected_filters)
    return {} unless selected_filters
    
    selected_filters.each_with_object({}) do |(field, values), filters|
      filters[field] = Array(values)
    end
  end
end

# app/search/faceted_search_result.rb
class FacetedSearchResult
  attr_reader :search_result, :facets

  def initialize(search_result, facet_config)
    @search_result = search_result
    @facet_config = facet_config
    @facets = build_facets
  end

  def results
    @search_result.results
  end

  def total_count
    @search_result.total_count
  end

  def aggregations
    @search_result.aggregations
  end

  private

  def build_facets
    @facet_config.each_with_object({}) do |(name, config), facets|
      aggregation = @search_result.aggregations[name]
      facets[name] = Facet.new(name, config, aggregation)
    end
  end
end

# app/search/facet.rb
class Facet
  attr_reader :name, :config, :aggregation

  def initialize(name, config, aggregation)
    @name = name
    @config = config
    @aggregation = aggregation
  end

  def values
    return [] unless @aggregation
    
    @aggregation['buckets'].map do |bucket|
      FacetValue.new(
        value: bucket['key'],
        count: bucket['doc_count'],
        selected: false
      )
    end
  end

  def selected_values
    values.select(&:selected?)
  end
end

# app/search/facet_value.rb
class FacetValue
  attr_reader :value, :count, :selected

  def initialize(value:, count:, selected:)
    @value = value
    @count = count
    @selected = selected
  end

  def selected?
    @selected
  end
end
```

### Search Analytics

```ruby
# app/search/analytics/search_analytics.rb
class SearchAnalytics
  include Singleton

  def initialize
    @redis = Redis.new
  end

  def track_search(query, result_count, duration, user_id = nil)
    timestamp = Time.current
    
    # Track search query
    track_query(query, timestamp)
    
    # Track search metrics
    track_metrics(query, result_count, duration, timestamp)
    
    # Track user behavior
    track_user_behavior(user_id, query, result_count, timestamp) if user_id
  end

  def track_click(query, document_id, position, user_id = nil)
    timestamp = Time.current
    
    # Track click
    track_click_event(query, document_id, position, timestamp)
    
    # Track user click behavior
    track_user_click(user_id, query, document_id, position, timestamp) if user_id
  end

  def get_popular_queries(limit = 10)
    @redis.zrevrange('search:queries', 0, limit - 1, withscores: true)
  end

  def get_zero_result_queries(limit = 10)
    @redis.zrevrange('search:zero_results', 0, limit - 1, withscores: true)
  end

  def get_search_metrics(days = 7)
    end_date = Date.current
    start_date = end_date - days.days
    
    {
      total_searches: get_total_searches(start_date, end_date),
      average_results: get_average_results(start_date, end_date),
      average_duration: get_average_duration(start_date, end_date),
      zero_result_rate: get_zero_result_rate(start_date, end_date)
    }
  end

  private

  def track_query(query, timestamp)
    key = "search:queries"
    @redis.zincrby(key, 1, query.downcase)
    @redis.expire(key, 30.days.to_i)
  end

  def track_metrics(query, result_count, duration, timestamp)
    # Track result count
    result_key = "search:results:#{query.downcase}"
    @redis.lpush(result_key, result_count)
    @redis.ltrim(result_key, 0, 99) # Keep last 100 searches
    @redis.expire(result_key, 30.days.to_i)
    
    # Track duration
    duration_key = "search:duration:#{query.downcase}"
    @redis.lpush(duration_key, duration)
    @redis.ltrim(duration_key, 0, 99)
    @redis.expire(duration_key, 30.days.to_i)
    
    # Track zero results
    if result_count == 0
      zero_key = "search:zero_results"
      @redis.zincrby(zero_key, 1, query.downcase)
      @redis.expire(zero_key, 30.days.to_i)
    end
  end

  def track_user_behavior(user_id, query, result_count, timestamp)
    key = "user:search:#{user_id}"
    @redis.lpush(key, {
      query: query,
      result_count: result_count,
      timestamp: timestamp.iso8601
    }.to_json)
    @redis.ltrim(key, 0, 99)
    @redis.expire(key, 30.days.to_i)
  end

  def track_click_event(query, document_id, position, timestamp)
    key = "search:clicks:#{query.downcase}"
    @redis.lpush(key, {
      document_id: document_id,
      position: position,
      timestamp: timestamp.iso8601
    }.to_json)
    @redis.ltrim(key, 0, 99)
    @redis.expire(key, 30.days.to_i)
  end

  def track_user_click(user_id, query, document_id, position, timestamp)
    key = "user:clicks:#{user_id}"
    @redis.lpush(key, {
      query: query,
      document_id: document_id,
      position: position,
      timestamp: timestamp.iso8601
    }.to_json)
    @redis.ltrim(key, 0, 99)
    @redis.expire(key, 30.days.to_i)
  end

  def get_total_searches(start_date, end_date)
    # Implementation would count searches in date range
    0
  end

  def get_average_results(start_date, end_date)
    # Implementation would calculate average results in date range
    0
  end

  def get_average_duration(start_date, end_date)
    # Implementation would calculate average duration in date range
    0
  end

  def get_zero_result_rate(start_date, end_date)
    # Implementation would calculate zero result rate in date range
    0
  end
end
```

## Performance Optimization

### Search Caching

```ruby
# app/search/caching/search_cache.rb
class SearchCache
  include Singleton

  def initialize
    @redis = Redis.new
    @config = Rails.application.config.search
  end

  def get(query, options = {})
    key = cache_key(query, options)
    cached = @redis.get(key)
    
    if cached
      JSON.parse(cached)
    else
      nil
    end
  end

  def set(query, options = {}, result)
    key = cache_key(query, options)
    ttl = cache_ttl(query, options)
    
    @redis.setex(key, ttl, result.to_json)
  end

  def invalidate(pattern = nil)
    if pattern
      keys = @redis.keys("search:cache:#{pattern}")
      @redis.del(*keys) if keys.any?
    else
      keys = @redis.keys("search:cache:*")
      @redis.del(*keys) if keys.any?
    end
  end

  private

  def cache_key(query, options)
    options_str = options.sort.to_h.to_json
    "search:cache:#{Digest::MD5.hexdigest("#{query}#{options_str}")}"
  end

  def cache_ttl(query, options)
    # Longer TTL for popular queries
    if popular_query?(query)
      1.hour.to_i
    else
      15.minutes.to_i
    end
  end

  def popular_query?(query)
    # Check if query is in top searches
    rank = @redis.zrevrank('search:queries', query.downcase)
    rank && rank < 100
  end
end
```

## Testing

### Search Test Helper

```ruby
# spec/support/search_helper.rb
module SearchHelper
  def index_test_data
    # Index test data for search tests
    User.all.each { |user| SearchService.instance.index_user(user) }
    Post.all.each { |post| SearchService.instance.index_post(post) }
  end

  def clear_search_indexes
    SearchManager.instance.delete_index('users')
    SearchManager.instance.delete_index('posts')
  end

  def expect_search_results(query, expected_count)
    result = SearchService.instance.search_all(query)
    expect(result.total_count).to eq(expected_count)
  end

  def expect_search_suggestions(query, expected_suggestions)
    suggestions = SearchService.instance.suggest_all(query)
    expect(suggestions).to include(*expected_suggestions)
  end
end

RSpec.configure do |config|
  config.include SearchHelper, type: :search
  
  config.before(:each, type: :search) do
    clear_search_indexes
  end
  
  config.after(:each, type: :search) do
    index_test_data
  end
end
```

### Search Tests

```ruby
# spec/search/search_service_spec.rb
RSpec.describe SearchService, type: :search do
  let(:service) { SearchService.instance }
  let(:user) { create(:user, name: 'John Doe', email: 'john@example.com') }
  let(:post) { create(:post, title: 'Ruby on Rails Guide', content: 'Learn Ruby on Rails') }

  before do
    index_test_data
  end

  describe '#search_users' do
    it 'finds users by name' do
      result = service.search_users('John')
      expect(result.total_count).to eq(1)
      expect(result.results.first[:source]['name']).to eq('John Doe')
    end

    it 'finds users by email' do
      result = service.search_users('john@example.com')
      expect(result.total_count).to eq(1)
      expect(result.results.first[:source]['email']).to eq('john@example.com')
    end
  end

  describe '#search_posts' do
    it 'finds posts by title' do
      result = service.search_posts('Rails')
      expect(result.total_count).to eq(1)
      expect(result.results.first[:source]['title']).to eq('Ruby on Rails Guide')
    end

    it 'finds posts by content' do
      result = service.search_posts('Learn Ruby')
      expect(result.total_count).to eq(1)
      expect(result.results.first[:source]['content']).to eq('Learn Ruby on Rails')
    end
  end

  describe '#suggest_users' do
    it 'suggests user names' do
      suggestions = service.suggest_users('Jo')
      expect(suggestions).to include('John')
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # Search configuration
  config.search = {
    backend: ENV['SEARCH_BACKEND'] || 'elasticsearch',
    elasticsearch: {
      url: ENV['ELASTICSEARCH_URL'] || 'http://localhost:9200',
      index_prefix: ENV['ELASTICSEARCH_INDEX_PREFIX'] || 'tusk_',
      number_of_shards: ENV['ELASTICSEARCH_SHARDS'] || 1,
      number_of_replicas: ENV['ELASTICSEARCH_REPLICAS'] || 0,
      refresh_interval: ENV['ELASTICSEARCH_REFRESH_INTERVAL'] || '1s',
      bulk_size: ENV['ELASTICSEARCH_BULK_SIZE'] || 1000,
      timeout: ENV['ELASTICSEARCH_TIMEOUT'] || 30
    },
    solr: {
      url: ENV['SOLR_URL'] || 'http://localhost:8983/solr',
      core: ENV['SOLR_CORE'] || 'tusk_core',
      timeout: ENV['SOLR_TIMEOUT'] || 30,
      batch_size: ENV['SOLR_BATCH_SIZE'] || 1000
    },
    postgresql: {
      dictionary: ENV['POSTGRESQL_DICTIONARY'] || 'english',
      trigram_similarity_threshold: ENV['POSTGRESQL_TRIGRAM_THRESHOLD'] || 0.3,
      full_text_search_enabled: ENV['POSTGRESQL_FULL_TEXT_ENABLED'] != 'false'
    },
    indexing: {
      auto_index: ENV['SEARCH_AUTO_INDEX'] != 'false',
      batch_size: ENV['SEARCH_BATCH_SIZE'] || 100,
      background_jobs: ENV['SEARCH_BACKGROUND_JOBS'] != 'false',
      real_time_updates: ENV['SEARCH_REAL_TIME_UPDATES'] != 'false'
    },
    search: {
      default_operator: ENV['SEARCH_DEFAULT_OPERATOR'] || 'AND',
      fuzzy_matching: ENV['SEARCH_FUZZY_MATCHING'] != 'false',
      highlight_enabled: ENV['SEARCH_HIGHLIGHT_ENABLED'] != 'false',
      suggest_enabled: ENV['SEARCH_SUGGEST_ENABLED'] != 'false',
      max_results: ENV['SEARCH_MAX_RESULTS'] || 1000
    },
    monitoring: {
      enabled: ENV['SEARCH_MONITORING_ENABLED'] != 'false',
      metrics_port: ENV['SEARCH_METRICS_PORT'] || 9090,
      health_check_interval: ENV['SEARCH_HEALTH_CHECK_INTERVAL'] || 30
    }
  }
end
```

### Docker Configuration

```dockerfile
# Dockerfile.search
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    elasticsearch

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

CMD ["bundle", "exec", "ruby", "app/search/search_runner.rb"]
```

```yaml
# docker-compose.search.yml
version: '3.8'

services:
  search-service:
    build:
      context: .
      dockerfile: Dockerfile.search
    environment:
      - RAILS_ENV=production
      - ELASTICSEARCH_URL=http://elasticsearch:9200
      - SEARCH_BACKEND=elasticsearch
    depends_on:
      - elasticsearch
      - redis

  elasticsearch:
    image: elasticsearch:8.8.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  elasticsearch_data:
  redis_data:
```

This comprehensive search integration guide provides everything needed to build powerful search capabilities with TuskLang and Ruby, including multiple backend support, advanced querying, faceted search, analytics, performance optimization, testing, and deployment strategies. 