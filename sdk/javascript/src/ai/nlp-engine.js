/**
 * Natural Language Processing Engine for TuskTsk
 * Provides comprehensive NLP capabilities with TensorFlow.js and natural library
 * 
 * @author AI-Cloud-Native-Engineer
 * @version 2.0.3
 * @license BBL
 */

const natural = require('natural');
const compromise = require('compromise');
const tf = require('@tensorflow/tfjs-node');
const fs = require('fs').promises;
const path = require('path');

class NLPEngine {
    constructor() {
        this.tokenizers = new Map();
        this.classifiers = new Map();
        this.sentimentAnalyzers = new Map();
        this.languageDetectors = new Map();
        this.textProcessors = new Map();
        this.isInitialized = false;
        this.supportedLanguages = ['en', 'es', 'fr', 'de', 'it', 'pt', 'ru', 'ja', 'ko', 'zh'];
    }

    /**
     * Initialize NLP engine with required components
     */
    async initialize() {
        try {
            // Initialize tokenizers
            this.tokenizers.set('word', new natural.WordTokenizer());
            this.tokenizers.set('sentence', new natural.SentenceTokenizer());
            this.tokenizers.set('aggressive', new natural.AggressiveTokenizer());
            this.tokenizers.set('case', new natural.CaseTokenizer());
            this.tokenizers.set('regexp', new natural.RegexpTokenizer({ pattern: /\s+/ }));

            // Initialize stemmers
            this.stemmers = {
                porter: natural.PorterStemmer,
                lancaster: natural.LancasterStemmer,
                lovins: natural.LovinsStemmer
            };

            // Initialize lemmatizers
            this.lemmatizers = {
                wordnet: natural.WordNetLemmatizer,
                wnl: natural.WordNetLemmatizer
            };

            // Initialize TF-IDF
            this.tfidf = new natural.TfIdf();

            // Initialize language detection (simplified)
            this.languageDetector = {
                detect: (text) => {
                    // Simple language detection based on common words
                    const englishWords = ['the', 'and', 'or', 'but', 'in', 'on', 'at', 'to', 'for', 'of', 'with', 'by'];
                    const spanishWords = ['el', 'la', 'de', 'que', 'y', 'a', 'en', 'un', 'es', 'se', 'no', 'te'];
                    const frenchWords = ['le', 'la', 'de', 'et', 'en', 'un', 'que', 'est', 'pour', 'dans', 'sur', 'avec'];
                    
                    const words = text.toLowerCase().split(/\s+/);
                    let scores = { en: 0, es: 0, fr: 0 };
                    
                    words.forEach(word => {
                        if (englishWords.includes(word)) scores.en++;
                        if (spanishWords.includes(word)) scores.es++;
                        if (frenchWords.includes(word)) scores.fr++;
                    });
                    
                    const maxScore = Math.max(scores.en, scores.es, scores.fr);
                    if (maxScore === 0) return 'en'; // default to English
                    
                    return Object.keys(scores).find(lang => scores[lang] === maxScore);
                }
            };

            this.isInitialized = true;
            console.log('✅ NLP Engine initialized successfully');
            
            return {
                success: true,
                tokenizers: Array.from(this.tokenizers.keys()),
                stemmers: Object.keys(this.stemmers),
                lemmatizers: Object.keys(this.lemmatizers),
                supportedLanguages: this.supportedLanguages
            };
        } catch (error) {
            console.error('❌ NLP Engine initialization failed:', error);
            throw new Error(`NLP Engine initialization failed: ${error.message}`);
        }
    }

    /**
     * Tokenize text using specified tokenizer
     */
    tokenize(text, tokenizerType = 'word') {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        if (!this.tokenizers.has(tokenizerType)) {
            throw new Error(`Tokenizer '${tokenizerType}' not found`);
        }

        try {
            const tokenizer = this.tokenizers.get(tokenizerType);
            const tokens = tokenizer.tokenize(text);
            
            return {
                success: true,
                tokens: tokens,
                count: tokens.length,
                tokenizer: tokenizerType
            };
        } catch (error) {
            console.error(`❌ Tokenization failed: ${error.message}`);
            throw new Error(`Tokenization failed: ${error.message}`);
        }
    }

    /**
     * Stem words using specified stemmer
     */
    stem(text, stemmerType = 'porter') {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        if (!this.stemmers[stemmerType]) {
            throw new Error(`Stemmer '${stemmerType}' not found`);
        }

        try {
            const stemmer = this.stemmers[stemmerType];
            const tokens = this.tokenize(text).tokens;
            const stemmedTokens = tokens.map(token => stemmer.stem(token));
            
            return {
                success: true,
                original: tokens,
                stemmed: stemmedTokens,
                stemmer: stemmerType
            };
        } catch (error) {
            console.error(`❌ Stemming failed: ${error.message}`);
            throw new Error(`Stemming failed: ${error.message}`);
        }
    }

    /**
     * Lemmatize words
     */
    lemmatize(text, lemmatizerType = 'wordnet') {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        try {
            const tokens = this.tokenize(text).tokens;
            const lemmatizedTokens = tokens.map(token => {
                return natural.WordNetLemmatizer.lemmatize(token);
            });
            
            return {
                success: true,
                original: tokens,
                lemmatized: lemmatizedTokens,
                lemmatizer: lemmatizerType
            };
        } catch (error) {
            console.error(`❌ Lemmatization failed: ${error.message}`);
            throw new Error(`Lemmatization failed: ${error.message}`);
        }
    }

    /**
     * Perform sentiment analysis
     */
    analyzeSentiment(text, method = 'afinn') {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        try {
            let sentiment = null;
            
            switch (method.toLowerCase()) {
                case 'afinn':
                    sentiment = natural.Afinn.analyze(text);
                    break;
                case 'sentiment':
                    sentiment = natural.Sentiment.analyze(text);
                    break;
                case 'vader':
                    sentiment = natural.VaderSentiment.analyze(text);
                    break;
                default:
                    throw new Error(`Sentiment method '${method}' not supported`);
            }
            
            return {
                success: true,
                text: text,
                sentiment: sentiment,
                method: method
            };
        } catch (error) {
            console.error(`❌ Sentiment analysis failed: ${error.message}`);
            throw new Error(`Sentiment analysis failed: ${error.message}`);
        }
    }

    /**
     * Detect language of text
     */
    detectLanguage(text) {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        try {
            const language = this.languageDetector.detect(text);
            
            return {
                success: true,
                text: text,
                language: language,
                confidence: language.confidence || 0
            };
        } catch (error) {
            console.error(`❌ Language detection failed: ${error.message}`);
            throw new Error(`Language detection failed: ${error.message}`);
        }
    }

    /**
     * Extract named entities from text
     */
    extractEntities(text) {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        try {
            const doc = compromise(text);
            
            const entities = {
                people: doc.people().out('array'),
                places: doc.places().out('array'),
                organizations: doc.organizations().out('array'),
                dates: doc.dates().out('array'),
                times: doc.times().out('array'),
                numbers: doc.numbers().out('array'),
                money: doc.money().out('array'),
                urls: doc.urls().out('array'),
                emails: doc.emails().out('array'),
                phoneNumbers: doc.phoneNumbers().out('array')
            };
            
            return {
                success: true,
                text: text,
                entities: entities,
                totalEntities: Object.values(entities).flat().length
            };
        } catch (error) {
            console.error(`❌ Entity extraction failed: ${error.message}`);
            throw new Error(`Entity extraction failed: ${error.message}`);
        }
    }

    /**
     * Extract key phrases from text
     */
    extractKeyPhrases(text, maxPhrases = 10) {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        try {
            // Tokenize and remove stop words
            const tokens = this.tokenize(text, 'word').tokens;
            const filteredTokens = tokens.filter(token => 
                !natural.stopwords.includes(token.toLowerCase())
            );
            
            // Calculate TF-IDF scores
            this.tfidf.addDocument(filteredTokens.join(' '));
            const scores = this.tfidf.listTerms(0);
            
            // Extract top phrases
            const keyPhrases = scores
                .slice(0, maxPhrases)
                .map(item => ({
                    term: item.term,
                    score: item.score,
                    count: item.count
                }));
            
            return {
                success: true,
                text: text,
                keyPhrases: keyPhrases,
                totalPhrases: keyPhrases.length
            };
        } catch (error) {
            console.error(`❌ Key phrase extraction failed: ${error.message}`);
            throw new Error(`Key phrase extraction failed: ${error.message}`);
        }
    }

    /**
     * Calculate text similarity between two texts
     */
    calculateSimilarity(text1, text2, method = 'cosine') {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        try {
            let similarity = 0;
            
            switch (method.toLowerCase()) {
                case 'cosine':
                    similarity = natural.JaroWinklerDistance(text1, text2);
                    break;
                case 'jaro':
                    similarity = natural.JaroWinklerDistance(text1, text2);
                    break;
                case 'levenshtein':
                    similarity = natural.LevenshteinDistance(text1, text2);
                    break;
                case 'dice':
                    similarity = natural.DiceCoefficient(text1, text2);
                    break;
                default:
                    throw new Error(`Similarity method '${method}' not supported`);
            }
            
            return {
                success: true,
                text1: text1,
                text2: text2,
                similarity: similarity,
                method: method
            };
        } catch (error) {
            console.error(`❌ Similarity calculation failed: ${error.message}`);
            throw new Error(`Similarity calculation failed: ${error.message}`);
        }
    }

    /**
     * Create a text classifier
     */
    createClassifier(name, trainingData) {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        try {
            const classifier = new natural.BayesClassifier();
            
            // Add training data
            trainingData.forEach(item => {
                classifier.addDocument(item.text, item.category);
            });
            
            // Train the classifier
            classifier.train();
            
            // Store the classifier
            this.classifiers.set(name, {
                classifier: classifier,
                trainingData: trainingData,
                created: new Date()
            });
            
            console.log(`✅ Classifier '${name}' created successfully`);
            return {
                success: true,
                name: name,
                categories: classifier.getLabels(),
                trainingSamples: trainingData.length
            };
        } catch (error) {
            console.error(`❌ Classifier creation failed: ${error.message}`);
            throw new Error(`Classifier creation failed: ${error.message}`);
        }
    }

    /**
     * Classify text using a trained classifier
     */
    classifyText(text, classifierName) {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        if (!this.classifiers.has(classifierName)) {
            throw new Error(`Classifier '${classifierName}' not found`);
        }

        try {
            const classifierInfo = this.classifiers.get(classifierName);
            const classification = classifierInfo.classifier.classify(text);
            const scores = classifierInfo.classifier.getClassifications(text);
            
            return {
                success: true,
                text: text,
                classification: classification,
                scores: scores,
                classifier: classifierName
            };
        } catch (error) {
            console.error(`❌ Text classification failed: ${error.message}`);
            throw new Error(`Text classification failed: ${error.message}`);
        }
    }

    /**
     * Perform comprehensive text analysis
     */
    analyzeText(text, options = {}) {
        if (!this.isInitialized) {
            throw new Error('NLP Engine not initialized. Call initialize() first.');
        }

        const {
            includeSentiment = true,
            includeEntities = true,
            includeKeyPhrases = true,
            includeLanguage = true,
            sentimentMethod = 'afinn',
            maxPhrases = 10
        } = options;

        try {
            const analysis = {
                text: text,
                timestamp: new Date(),
                wordCount: this.tokenize(text).count,
                characterCount: text.length
            };

            // Sentiment analysis
            if (includeSentiment) {
                analysis.sentiment = this.analyzeSentiment(text, sentimentMethod);
            }

            // Entity extraction
            if (includeEntities) {
                analysis.entities = this.extractEntities(text);
            }

            // Key phrase extraction
            if (includeKeyPhrases) {
                analysis.keyPhrases = this.extractKeyPhrases(text, maxPhrases);
            }

            // Language detection
            if (includeLanguage) {
                analysis.language = this.detectLanguage(text);
            }

            return {
                success: true,
                analysis: analysis
            };
        } catch (error) {
            console.error(`❌ Text analysis failed: ${error.message}`);
            throw new Error(`Text analysis failed: ${error.message}`);
        }
    }

    /**
     * Get available classifiers
     */
    listClassifiers() {
        return Array.from(this.classifiers.keys()).map(name => {
            const info = this.classifiers.get(name);
            return {
                name: name,
                categories: info.classifier.getLabels(),
                trainingSamples: info.trainingData.length,
                created: info.created
            };
        });
    }

    /**
     * Delete a classifier
     */
    deleteClassifier(name) {
        if (!this.classifiers.has(name)) {
            throw new Error(`Classifier '${name}' not found`);
        }

        this.classifiers.delete(name);
        console.log(`✅ Classifier '${name}' deleted`);
        
        return {
            success: true,
            name: name
        };
    }

    /**
     * Get NLP engine information
     */
    getInfo() {
        return {
            initialized: this.isInitialized,
            tokenizers: Array.from(this.tokenizers.keys()),
            stemmers: Object.keys(this.stemmers),
            lemmatizers: Object.keys(this.lemmatizers),
            classifiers: this.classifiers.size,
            supportedLanguages: this.supportedLanguages
        };
    }

    /**
     * Clean up resources
     */
    cleanup() {
        this.tokenizers.clear();
        this.classifiers.clear();
        this.sentimentAnalyzers.clear();
        this.languageDetectors.clear();
        this.textProcessors.clear();
        
        console.log('✅ NLP Engine resources cleaned up');
    }
}

// Export the class
module.exports = NLPEngine;

// Create a singleton instance
const nlpEngine = new NLPEngine();

// Export the singleton instance
module.exports.instance = nlpEngine; 