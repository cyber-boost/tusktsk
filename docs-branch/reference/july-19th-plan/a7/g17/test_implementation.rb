#!/usr/bin/env ruby
require_relative 'goal_implementation'
require 'test/unit'

class TestAIFramework < Test::Unit::TestCase
  def setup
    @ai = AIFramework.new
  end

  def test_neural_network_initialization
    nn = NeuralNetwork.new([3, 4, 2])
    assert_equal 2, nn.instance_variable_get(:@weights).length
    
    # First layer: 3->4 connections
    assert_equal 3, nn.instance_variable_get(:@weights)[0].length
    assert_equal 4, nn.instance_variable_get(:@weights)[0][0].length
    
    # Second layer: 4->2 connections
    assert_equal 4, nn.instance_variable_get(:@weights)[1].length
    assert_equal 2, nn.instance_variable_get(:@weights)[1][0].length
  end

  def test_neural_network_forward_pass
    nn = NeuralNetwork.new([2, 3, 1])
    input = [0.5, 0.8]
    output = nn.forward(input)
    
    assert_instance_of Array, output
    assert_equal 1, output.length
    assert output[0] >= 0 && output[0] <= 1, "Output should be between 0 and 1 (sigmoid)"
  end

  def test_neural_network_training
    nn = NeuralNetwork.new([2, 2, 1])
    inputs = [[0.1, 0.2], [0.3, 0.4]]
    targets = [[0.8], [0.9]]
    
    # Should not raise errors during training
    assert_nothing_raised do
      nn.train(inputs, targets, 10) # Short training for test
    end
  end

  def test_nlp_tokenization
    text = "Hello, World! This is a test."
    tokens = @ai.nlp.tokenize(text)
    
    expected = ['hello', 'world', 'this', 'is', 'a', 'test']
    assert_equal expected, tokens
  end

  def test_nlp_vocabulary_building
    texts = [
      "I love programming",
      "Programming is amazing",
      "I love amazing code"
    ]
    
    @ai.nlp.build_vocabulary(texts)
    vocab = @ai.nlp.instance_variable_get(:@vocabulary)
    
    assert_equal 2, vocab['i']
    assert_equal 2, vocab['love']
    assert_equal 2, vocab['programming']
    assert_equal 2, vocab['amazing']
    assert_equal 1, vocab['is']
    assert_equal 1, vocab['code']
  end

  def test_sentiment_analysis
    positive_text = "This is great and awesome!"
    negative_text = "This is terrible and awful!"
    neutral_text = "This is a chair."
    
    assert_equal :positive, @ai.nlp.sentiment_analysis(positive_text)
    assert_equal :negative, @ai.nlp.sentiment_analysis(negative_text)
    assert_equal :neutral, @ai.nlp.sentiment_analysis(neutral_text)
  end

  def test_text_similarity
    text1 = "I love Ruby programming"
    text2 = "Ruby programming is love"
    text3 = "Python is different"
    
    similarity_high = @ai.nlp.text_similarity(text1, text2)
    similarity_low = @ai.nlp.text_similarity(text1, text3)
    
    assert similarity_high > similarity_low, "Similar texts should have higher similarity"
    assert similarity_high >= 0 && similarity_high <= 1, "Similarity should be between 0 and 1"
    assert similarity_low >= 0 && similarity_low <= 1, "Similarity should be between 0 and 1"
  end

  def test_computer_vision_edge_detection
    # Create a simple 3x3 test image
    image = [
      [100, 100, 200],
      [100, 100, 200],
      [100, 100, 200]
    ]
    
    edges = @ai.vision.edge_detection(image)
    
    assert_equal 3, edges.length
    assert_equal 3, edges[0].length
    assert_instance_of Float, edges[1][1]
    
    # Middle pixel should have some edge response due to the vertical edge
    assert edges[1][1] > 0, "Should detect vertical edge"
  end

  def test_computer_vision_object_detection
    # Create a test image with high intensity region (simulating an object)
    image = Array.new(30) { Array.new(30, 50) } # Low intensity background
    
    # Add high intensity "object" in center
    (10..19).each do |i|
      (10..19).each do |j|
        image[i][j] = 200
      end
    end
    
    objects = @ai.vision.object_detection(image)
    
    assert_instance_of Array, objects
    refute_empty objects, "Should detect at least one object"
    
    object = objects.first
    assert object.key?(:x)
    assert object.key?(:y)
    assert object.key?(:width)
    assert object.key?(:height)
    assert object.key?(:confidence)
    assert object[:confidence] > 0 && object[:confidence] <= 1
  end

  def test_ai_framework_integration
    assert_instance_of NeuralNetwork, @ai.neural_net
    assert_instance_of NLPProcessor, @ai.nlp
    assert_instance_of ComputerVision, @ai.vision
  end

  def test_multimodal_processing
    text = "This is amazing and wonderful!"
    
    # Create test image with objects
    image = Array.new(30) { Array.new(30, 50) }
    (5..14).each { |i| (5..14).each { |j| image[i][j] = 200 } }
    (15..24).each { |i| (15..24).each { |j| image[i][j] = 200 } }
    
    result = @ai.process_multimodal(text, image)
    
    assert result.key?(:text_sentiment)
    assert result.key?(:detected_objects)
    assert result.key?(:combined_score)
    
    assert_equal :positive, result[:text_sentiment]
    assert result[:detected_objects] >= 1, "Should detect objects"
    assert result[:combined_score] > 1, "Should have positive combined score"
  end

  def test_neural_network_matrix_multiplication
    nn = NeuralNetwork.new([2, 2])
    
    # Test matrix multiplication method
    a = [[1, 2], [3, 4]]
    b = [[5, 6], [7, 8]]
    
    result = nn.send(:matrix_mult, a, b)
    expected = [[19, 22], [43, 50]]
    
    assert_equal expected, result
  end

  def test_computer_vision_kernel_application
    cv = ComputerVision.new
    
    image = [
      [1, 2, 3],
      [4, 5, 6],
      [7, 8, 9]
    ]
    
    kernel = [
      [1, 0, -1],
      [1, 0, -1],
      [1, 0, -1]
    ]
    
    result = cv.send(:apply_kernel, image, kernel, 1, 1)
    expected = (1*1 + 2*0 + 3*(-1) + 4*1 + 5*0 + 6*(-1) + 7*1 + 8*0 + 9*(-1))
    
    assert_equal expected, result
  end

  def test_computer_vision_region_extraction
    cv = ComputerVision.new
    
    image = [
      [1, 2, 3, 4],
      [5, 6, 7, 8],
      [9, 10, 11, 12],
      [13, 14, 15, 16]
    ]
    
    region = cv.send(:extract_region, image, 1, 1, 2, 2)
    expected = [[6, 7], [10, 11]]
    
    assert_equal expected, region
  end

  def test_edge_case_empty_text
    result = @ai.nlp.sentiment_analysis("")
    assert_equal :neutral, result
    
    similarity = @ai.nlp.text_similarity("", "test")
    assert_equal 0.0, similarity
  end

  def test_edge_case_small_image
    small_image = [[100]]
    edges = @ai.vision.edge_detection(small_image)
    assert_equal 1, edges.length
    assert_equal 1, edges[0].length
  end
end

if __FILE__ == $0
  puts "🔥 RUNNING G17 PRODUCTION TESTS..."
  Test::Unit::AutoRunner.run
end 