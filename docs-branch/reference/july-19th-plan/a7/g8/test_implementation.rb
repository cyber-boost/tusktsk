#!/usr/bin/env ruby
# frozen_string_literal: true

# Test Implementation for TuskLang Ruby SDK - Goal 8
# Comprehensive testing of AI and machine learning features

require_relative 'goal_implementation'
require 'test/unit'

class TestGoal8Implementation < Test::Unit::TestCase
  def setup
    @coordinator = TuskLang::Goal8::Goal8Coordinator.new
    @neural_network = @coordinator.neural_network
    @nlp_engine = @coordinator.nlp_engine
    @computer_vision = @coordinator.computer_vision
  end

  # Test G8.1: Advanced Ruby AI Neural Network and Deep Learning Framework
  def test_neural_network
    # Test network creation
    @neural_network.create_network([2, 4, 1], [:relu, :sigmoid])
    
    assert_equal [2, 4, 1], @neural_network.layers
    assert_equal 2, @neural_network.weights.length
    assert_equal 2, @neural_network.biases.length

    # Test forward propagation
    input_data = [0.5, 0.3]
    activations = @neural_network.forward_propagate(input_data)
    
    assert_equal 3, activations.length  # Input + 2 hidden layers
    assert_equal 1, activations.last.row_count  # Output layer has 1 neuron

    # Test training
    training_data = [[0, 0], [0, 1], [1, 0], [1, 1]]
    targets = [[0], [1], [1], [0]]
    
    @neural_network.train(training_data, targets, 100)
    
    assert @neural_network.training_history.length > 0
    assert @neural_network.training_history.last[:loss] < 1.0

    # Test predictions
    prediction = @neural_network.predict([0, 1])
    assert prediction.is_a?(Array)
    assert_equal 1, prediction.length
    assert prediction[0].between?(0, 1)  # Sigmoid output should be between 0 and 1

    # Test model persistence
    test_file = 'test_model.json'
    @neural_network.save_model(test_file)
    assert File.exist?(test_file)
    
    # Load model
    new_network = TuskLang::Goal8::RubyAINeuralNetwork.new
    new_network.load_model(test_file)
    
    assert_equal @neural_network.layers, new_network.layers
    assert_equal @neural_network.weights.length, new_network.weights.length
    
    # Cleanup
    File.delete(test_file) if File.exist?(test_file)
  end

  # Test G8.2: Advanced Ruby Natural Language Processing and Text Analytics
  def test_nlp_engine
    # Test text preprocessing
    text = "Hello, World! This is a TEST."
    tokens = @nlp_engine.preprocess_text(text, { lowercase: true, remove_special_chars: true })
    
    assert tokens.is_a?(Array)
    assert tokens.all? { |token| token =~ /^[a-z0-9]+$/ }

    # Test vocabulary building
    texts = [
      "I love this product",
      "This product is amazing",
      "I hate this product"
    ]
    
    @nlp_engine.build_vocabulary(texts, 1)
    assert @nlp_engine.vocabulary.length > 0
    assert @nlp_engine.vocabulary.key?('product')

    # Test TF-IDF vectorization
    tfidf_vectors = @nlp_engine.tfidf_vectorize(texts)
    assert_equal texts.length, tfidf_vectors.length
    assert tfidf_vectors.all? { |vector| vector.is_a?(Array) }

    # Test sentiment analysis
    positive_text = "I love this amazing product!"
    negative_text = "I hate this terrible product."
    
    positive_sentiment = @nlp_engine.analyze_sentiment(positive_text)
    negative_sentiment = @nlp_engine.analyze_sentiment(negative_text)
    
    assert_equal 'positive', positive_sentiment[:sentiment]
    assert_equal 'negative', negative_sentiment[:sentiment]
    assert positive_sentiment[:score] > negative_sentiment[:score]

    # Test text similarity
    text1 = "I love this product"
    text2 = "I really like this product"
    text3 = "I hate this product"
    
    similarity1 = @nlp_engine.calculate_similarity(text1, text2)
    similarity2 = @nlp_engine.calculate_similarity(text1, text3)
    
    assert similarity1 > similarity2  # Similar texts should have higher similarity

    # Test entity extraction
    entity_text = "John Smith works at Apple Inc. in New York on 2024-01-15."
    entities = @nlp_engine.extract_entities(entity_text)
    
    assert entities.is_a?(Hash)
    assert entities.key?(:people)
    assert entities.key?(:organizations)
    assert entities.key?(:locations)
    assert entities.key?(:dates)

    # Test language detection
    english_text = "The quick brown fox jumps over the lazy dog."
    spanish_text = "El rápido zorro marrón salta sobre el perro perezoso."
    
    english_detection = @nlp_engine.detect_language(english_text)
    spanish_detection = @nlp_engine.detect_language(spanish_text)
    
    assert_equal :english, english_detection[:language]
    assert_equal :spanish, spanish_detection[:language]
  end

  # Test G8.3: Advanced Ruby Computer Vision and Image Processing
  def test_computer_vision
    # Create test image data (3x3 RGB)
    test_image_data = [
      255, 0, 0,   0, 255, 0,   0, 0, 255,
      128, 128, 128, 255, 255, 255, 0, 0, 0,
      255, 255, 0, 255, 0, 255, 0, 255, 255
    ]
    
    # Test image loading
    image = @computer_vision.load_image(test_image_data, 3, 3, 3)
    
    assert_equal 3, image[:width]
    assert_equal 3, image[:height]
    assert_equal 3, image[:channels]
    assert_equal 3, image[:pixels].length
    assert_equal 3, image[:pixels][0].length

    # Test grayscale conversion
    gray_image = @computer_vision.apply_filter(image, :grayscale)
    
    assert_equal 1, gray_image[:channels]
    assert_equal image[:width], gray_image[:width]
    assert_equal image[:height], gray_image[:height]

    # Test Gaussian blur
    blurred_image = @computer_vision.apply_filter(image, :gaussian_blur, { kernel_size: 3, sigma: 1.0 })
    
    assert_equal image[:width], blurred_image[:width]
    assert_equal image[:height], blurred_image[:height]
    assert_equal image[:channels], blurred_image[:channels]

    # Test edge detection
    edge_image = @computer_vision.apply_filter(image, :edge_detection)
    
    assert_equal 1, edge_image[:channels]  # Edge detection produces grayscale
    assert_equal image[:width], edge_image[:width]
    assert_equal image[:height], edge_image[:height]

    # Test HOG feature extraction
    hog_features = @computer_vision.extract_hog_features(gray_image, 2, 2, 9)
    
    assert hog_features.is_a?(Array)
    assert hog_features.length > 0
    assert hog_features.all? { |f| f.is_a?(Numeric) }

    # Test color analysis
    color_analysis = @computer_vision.analyze_colors(image)
    
    assert color_analysis.key?(:dominant_colors)
    assert color_analysis.key?(:average_color)
    assert color_analysis.key?(:color_variance)
    assert color_analysis[:dominant_colors].is_a?(Array)

    # Test image segmentation
    segmented_image = @computer_vision.segment_image(gray_image, :threshold, { threshold: 128 })
    
    assert_equal 1, segmented_image[:channels]
    assert_equal image[:width], segmented_image[:width]
    assert_equal image[:height], segmented_image[:height]

    # Test k-means segmentation
    kmeans_image = @computer_vision.segment_image(gray_image, :kmeans, { clusters: 3 })
    
    assert_equal 1, kmeans_image[:channels]
    assert_equal image[:width], kmeans_image[:width]
    assert_equal image[:height], kmeans_image[:height]
  end

  # Test integration of all components
  def test_integration
    # Test complete goal execution
    result = @coordinator.execute_all_goals
    
    assert result[:success]
    assert result[:execution_time] > 0
    assert_equal ['g8.1', 'g8.2', 'g8.3'], result[:goals_completed]
    assert result[:implementation_status]

    # Verify all goals are marked as completed
    assert_equal :completed, result[:implementation_status][:g8_1][:status]
    assert_equal :completed, result[:implementation_status][:g8_2][:status]
    assert_equal :completed, result[:implementation_status][:g8_3][:status]

    # Verify specific features
    assert result[:implementation_status][:g8_1][:features].include?('Neural Network Framework')
    assert result[:implementation_status][:g8_2][:features].include?('Sentiment Analysis')
    assert result[:implementation_status][:g8_3][:features].include?('Image Processing')
  end

  # Test error handling
  def test_error_handling
    # Test neural network with invalid input
    assert_raise(ArgumentError) do
      @neural_network.forward_propagate([])  # Empty input
    end

    # Test NLP with empty text
    empty_tokens = @nlp_engine.preprocess_text("")
    assert_equal [], empty_tokens

    # Test computer vision with invalid image data
    assert_raise(ArgumentError) do
      @computer_vision.load_image([], 0, 0, 3)  # Invalid dimensions
    end
  end

  # Test performance characteristics
  def test_performance
    start_time = Time.now
    
    # Test neural network performance
    @neural_network.create_network([10, 20, 10, 1], [:relu, :relu, :sigmoid])
    training_data = Array.new(100) { [rand, rand] }
    targets = Array.new(100) { [rand] }
    
    @neural_network.train(training_data, targets, 10)
    
    training_time = Time.now - start_time
    assert training_time < 10.0, "Neural network training took too long: #{training_time}s"
    
    # Test NLP performance
    start_time = Time.now
    large_texts = Array.new(50) { "This is a sample text for testing natural language processing capabilities. " * 10 }
    
    @nlp_engine.build_vocabulary(large_texts)
    tfidf_vectors = @nlp_engine.tfidf_vectorize(large_texts)
    
    nlp_time = Time.now - start_time
    assert nlp_time < 5.0, "NLP processing took too long: #{nlp_time}s"
    
    # Test computer vision performance
    start_time = Time.now
    large_image_data = Array.new(100 * 100 * 3) { rand(256) }
    large_image = @computer_vision.load_image(large_image_data, 100, 100, 3)
    
    gray_image = @computer_vision.apply_filter(large_image, :grayscale)
    hog_features = @computer_vision.extract_hog_features(gray_image)
    
    cv_time = Time.now - start_time
    assert cv_time < 5.0, "Computer vision processing took too long: #{cv_time}s"
  end

  # Test advanced features
  def test_advanced_features
    # Test neural network with different activation functions
    @neural_network.create_network([2, 3, 1], [:tanh, :sigmoid])
    prediction = @neural_network.predict([0.5, 0.5])
    assert prediction[0].between?(-1, 1)  # Tanh output range

    # Test NLP with different preprocessing options
    text = "Hello, WORLD! This is a TEST."
    tokens_with_stemming = @nlp_engine.preprocess_text(text, { stemming: true })
    tokens_without_stemming = @nlp_engine.preprocess_text(text, { stemming: false })
    
    assert tokens_with_stemming != tokens_without_stemming

    # Test computer vision with different filter parameters
    test_image_data = Array.new(5 * 5 * 3) { rand(256) }
    image = @computer_vision.load_image(test_image_data, 5, 5, 3)
    
    blur1 = @computer_vision.apply_filter(image, :gaussian_blur, { kernel_size: 3, sigma: 1.0 })
    blur2 = @computer_vision.apply_filter(image, :gaussian_blur, { kernel_size: 5, sigma: 2.0 })
    
    # Different blur parameters should produce different results
    assert blur1[:pixels] != blur2[:pixels]
  end
end

# Run tests if executed directly
if __FILE__ == $0
  require 'test/unit/autorunner'
  Test::Unit::AutoRunner.run
end 