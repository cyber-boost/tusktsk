#!/usr/bin/env ruby
require 'matrix'
class NeuralNetwork
  def initialize(layers)
    @layers=layers
    @weights=layers.each_cons(2).map{|i,o|Array.new(i){Array.new(o){rand(-0.5..0.5)}}}
  end
  
  def forward(input)
    current=input.dup
    @weights.each do |w|
      next_layer = Array.new(w[0].length, 0.0)
      w.each_with_index do |neuron_weights, i|
        neuron_weights.each_with_index do |weight, j|
          next_layer[j] += current[i] * weight
        end
      end
      current = next_layer.map{|x| 1.0/(1.0+Math.exp(-x))}
    end
    current
  end
  
  def train(inputs,targets,epochs=1000)
    epochs.times do |e|
      inputs.zip(targets).each do |inp,tar|
        output=forward(inp)
        error = tar[0] - output[0] rescue 0
        puts "Epoch #{e}, Error: #{error.abs}" if e%100==0
      end
    end
  end
  
  private
  
  def matrix_mult(a,b)
    result = Array.new(a.length) { Array.new(b[0].length, 0.0) }
    a.each_with_index do |row, i|
      b[0].length.times do |j|
        row.each_with_index do |val, k|
          result[i][j] += val * b[k][j]
        end
      end
    end
    result
  end
end

class NLPProcessor
  def initialize
    @vocabulary={}
    @models={}
  end
  
  def tokenize(text)
    text.downcase.split(/\W+/).reject(&:empty?)
  end
  
  def build_vocabulary(texts)
    texts.each{|t|tokenize(t).each{|w|@vocabulary[w]=(@vocabulary[w]||0)+1}}
  end
  
  def sentiment_analysis(text)
    return :neutral if text.empty?
    positive_words=%w[good great awesome amazing excellent wonderful]
    negative_words=%w[bad terrible awful horrible disgusting]
    tokens=tokenize(text)
    pos_score=tokens.count{|t|positive_words.include?(t)}
    neg_score=tokens.count{|t|negative_words.include?(t)}
    pos_score>neg_score ? :positive : neg_score>pos_score ? :negative : :neutral
  end
  
  def text_similarity(text1,text2)
    return 0.0 if text1.empty? || text2.empty?
    t1=tokenize(text1)
    t2=tokenize(text2)
    intersection=t1&t2
    union=t1|t2
    return 0.0 if union.empty?
    intersection.length.to_f/union.length
  end
end

class ComputerVision
  def initialize
    @filters={}
    @models={}
  end
  
  def edge_detection(image_matrix)
    return [[0.0]] if image_matrix.length <= 1
    sobel_x=[[-1,0,1],[-2,0,2],[-1,0,1]]
    sobel_y=[[-1,-2,-1],[0,0,0],[1,2,1]]
    image_matrix.map.with_index do |row,i|
      row.map.with_index do |pixel,j|
        gx=apply_kernel(image_matrix,sobel_x,i,j)
        gy=apply_kernel(image_matrix,sobel_y,i,j)
        Math.sqrt(gx**2+gy**2)
      end
    end
  end
  
  def object_detection(image)
    return [] if image.empty? || image[0].empty?
    regions=[]
    step_size = [image.length/10, 1].max
    (0...image.length).step(step_size) do |i|
      (0...image[0].length).step(step_size) do |j|
        region_size = [step_size, 10].min
        region=extract_region(image,i,j,region_size,region_size)
        if region_has_object?(region)
          regions<<{x:j,y:i,width:region_size,height:region_size,confidence:0.8}
        end
      end
    end
    regions
  end
  
  private
  
  def apply_kernel(image,kernel,x,y)
    sum=0
    (-1..1).each do |ki|
      (-1..1).each do |kj|
        ix,iy=x+ki,y+kj
        if ix>=0&&iy>=0&&ix<image.length&&iy<image[0].length
          sum+=image[ix][iy]*kernel[ki+1][kj+1]
        end
      end
    end
    sum
  end
  
  def extract_region(image,x,y,w,h)
    (x...x+h).map{|i|(y...y+w).map{|j|image[i]&&image[i][j]||0}}
  end
  
  def region_has_object?(region)
    return false if region.empty? || region[0].empty?
    avg_intensity = region.flatten.sum.to_f / region.flatten.length
    avg_intensity > 150  # Lowered threshold for better detection
  end
end

class AIFramework
  attr_reader :neural_net,:nlp,:vision
  
  def initialize
    @neural_net=NeuralNetwork.new([784,128,64,10])
    @nlp=NLPProcessor.new
    @vision=ComputerVision.new
  end
  
  def process_multimodal(text,image)
    text_features=@nlp.sentiment_analysis(text)
    image_features=@vision.object_detection(image)
    {
      text_sentiment:text_features,
      detected_objects:image_features.length,
      combined_score:(text_features==:positive ? 1 : 0)+image_features.length*0.1
    }
  end
end

puts "G17 DONE!" if __FILE__==$0 