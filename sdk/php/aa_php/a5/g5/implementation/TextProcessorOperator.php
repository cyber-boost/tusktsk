<?php

declare(strict_types=1);

namespace TuskLang\A5\G5;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * TextProcessorOperator - Advanced text processing including NLP operations
 * 
 * Provides advanced text processing operations including natural language processing,
 * text analysis, sentiment analysis, keyword extraction, and linguistic operations.
 */
class TextProcessorOperator extends CoreOperator
{
    private array $stopWords;
    private array $commonWords;

    public function __construct()
    {
        $this->initializeWordLists();
    }

    public function getName(): string
    {
        return 'text_processor';
    }

    public function getDescription(): string 
    {
        return 'Advanced text processing including NLP operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'analyze', 'extract_keywords', 'sentiment_analysis', 'remove_stopwords',
            'tokenize', 'stem', 'lemmatize', 'n_grams', 'word_frequency',
            'readability', 'language_detect', 'summarize', 'extract_entities'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'analyze' => $this->analyzeText($params['text'] ?? ''),
            'extract_keywords' => $this->extractKeywords($params['text'] ?? '', $params['count'] ?? 10),
            'sentiment_analysis' => $this->sentimentAnalysis($params['text'] ?? ''),
            'remove_stopwords' => $this->removeStopwords($params['text'] ?? '', $params['language'] ?? 'en'),
            'tokenize' => $this->tokenize($params['text'] ?? '', $params['type'] ?? 'word'),
            'stem' => $this->stem($params['words'] ?? [], $params['language'] ?? 'en'),
            'lemmatize' => $this->lemmatize($params['words'] ?? [], $params['language'] ?? 'en'),
            'n_grams' => $this->generateNGrams($params['text'] ?? '', $params['n'] ?? 2),
            'word_frequency' => $this->wordFrequency($params['text'] ?? '', $params['limit'] ?? null),
            'readability' => $this->readabilityScore($params['text'] ?? ''),
            'language_detect' => $this->detectLanguage($params['text'] ?? ''),
            'summarize' => $this->summarizeText($params['text'] ?? '', $params['sentences'] ?? 3),
            'extract_entities' => $this->extractEntities($params['text'] ?? ''),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Comprehensive text analysis
     */
    private function analyzeText(string $text): array
    {
        if (empty($text)) {
            return $this->getEmptyAnalysis();
        }

        $sentences = $this->splitIntoSentences($text);
        $words = $this->tokenize($text, 'word')['tokens'];
        $paragraphs = array_filter(explode("\n\n", $text));

        $analysis = [
            'basic_stats' => [
                'characters' => mb_strlen($text),
                'characters_no_spaces' => mb_strlen(str_replace(' ', '', $text)),
                'words' => count($words),
                'sentences' => count($sentences),
                'paragraphs' => count($paragraphs),
                'lines' => substr_count($text, "\n") + 1
            ],
            'averages' => [
                'words_per_sentence' => count($sentences) > 0 ? round(count($words) / count($sentences), 2) : 0,
                'chars_per_word' => count($words) > 0 ? round(mb_strlen(str_replace(' ', '', $text)) / count($words), 2) : 0,
                'sentences_per_paragraph' => count($paragraphs) > 0 ? round(count($sentences) / count($paragraphs), 2) : 0
            ],
            'complexity' => $this->textComplexity($text, $words, $sentences),
            'language_features' => $this->analyzeLanguageFeatures($text),
            'readability' => $this->readabilityScore($text),
            'sentiment' => $this->sentimentAnalysis($text)
        ];

        return $analysis;
    }

    /**
     * Extract keywords from text
     */
    private function extractKeywords(string $text, int $count = 10): array
    {
        if (empty($text)) {
            return ['keywords' => [], 'method' => 'frequency'];
        }

        // Remove stopwords and get frequency
        $cleanText = $this->removeStopwords($text)['text'];
        $frequency = $this->wordFrequency($cleanText);
        
        // Extract top keywords
        $keywords = array_slice($frequency['frequency'], 0, $count);
        
        // Calculate keyword scores using TF-IDF approximation
        $scoredKeywords = [];
        $totalWords = array_sum($frequency['frequency']);
        
        foreach ($keywords as $word => $freq) {
            $tf = $freq / $totalWords;
            $score = $tf * log($totalWords / $freq); // Simple IDF approximation
            
            $scoredKeywords[] = [
                'word' => $word,
                'frequency' => $freq,
                'score' => round($score, 4),
                'percentage' => round(($freq / $totalWords) * 100, 2)
            ];
        }

        return [
            'keywords' => $scoredKeywords,
            'total_words' => $totalWords,
            'unique_words' => count($frequency['frequency']),
            'method' => 'tf_idf_approximation'
        ];
    }

    /**
     * Basic sentiment analysis
     */
    private function sentimentAnalysis(string $text): array
    {
        if (empty($text)) {
            return ['sentiment' => 'neutral', 'score' => 0, 'confidence' => 0];
        }

        $positiveWords = [
            'good', 'great', 'excellent', 'amazing', 'wonderful', 'fantastic', 
            'awesome', 'brilliant', 'perfect', 'love', 'like', 'happy', 
            'pleased', 'satisfied', 'best', 'outstanding', 'superb'
        ];
        
        $negativeWords = [
            'bad', 'terrible', 'awful', 'horrible', 'worst', 'hate', 'dislike',
            'angry', 'sad', 'disappointed', 'disgusted', 'annoyed', 'frustrated',
            'poor', 'inadequate', 'unsatisfied', 'terrible', 'dreadful'
        ];

        $words = array_map('strtolower', $this->tokenize($text, 'word')['tokens']);
        
        $positiveScore = 0;
        $negativeScore = 0;
        $positiveMatches = [];
        $negativeMatches = [];

        foreach ($words as $word) {
            if (in_array($word, $positiveWords)) {
                $positiveScore++;
                $positiveMatches[] = $word;
            } elseif (in_array($word, $negativeWords)) {
                $negativeScore++;
                $negativeMatches[] = $word;
            }
        }

        $totalScore = $positiveScore - $negativeScore;
        $totalWords = count($words);
        
        $sentiment = 'neutral';
        if ($totalScore > 0) {
            $sentiment = 'positive';
        } elseif ($totalScore < 0) {
            $sentiment = 'negative';
        }

        $confidence = $totalWords > 0 ? abs($totalScore) / $totalWords : 0;

        return [
            'sentiment' => $sentiment,
            'score' => $totalScore,
            'confidence' => round($confidence, 4),
            'positive_score' => $positiveScore,
            'negative_score' => $negativeScore,
            'positive_words' => array_unique($positiveMatches),
            'negative_words' => array_unique($negativeMatches),
            'total_sentiment_words' => $positiveScore + $negativeScore
        ];
    }

    /**
     * Remove stopwords from text
     */
    private function removeStopwords(string $text, string $language = 'en'): array
    {
        $words = $this->tokenize($text, 'word')['tokens'];
        $stopwords = $this->stopWords[$language] ?? $this->stopWords['en'];
        
        $filteredWords = [];
        $removedWords = [];
        
        foreach ($words as $word) {
            $lowerWord = mb_strtolower($word);
            if (!in_array($lowerWord, $stopwords)) {
                $filteredWords[] = $word;
            } else {
                $removedWords[] = $word;
            }
        }

        return [
            'text' => implode(' ', $filteredWords),
            'original_words' => count($words),
            'filtered_words' => count($filteredWords),
            'removed_words' => count($removedWords),
            'removed_list' => array_unique($removedWords),
            'language' => $language
        ];
    }

    /**
     * Tokenize text into words, sentences, or characters
     */
    private function tokenize(string $text, string $type = 'word'): array
    {
        $tokens = [];
        
        switch ($type) {
            case 'word':
                $tokens = preg_split('/\s+/', trim($text));
                $tokens = array_filter($tokens, fn($token) => !empty($token));
                break;
                
            case 'sentence':
                $tokens = $this->splitIntoSentences($text);
                break;
                
            case 'character':
                $tokens = mb_str_split($text, 1, 'UTF-8');
                break;
                
            case 'paragraph':
                $tokens = array_filter(explode("\n\n", $text));
                break;
                
            default:
                throw new InvalidArgumentException("Unsupported tokenization type: {$type}");
        }

        return [
            'tokens' => array_values($tokens),
            'count' => count($tokens),
            'type' => $type,
            'original_length' => mb_strlen($text)
        ];
    }

    /**
     * Basic stemming (simplified Porter stemmer)
     */
    private function stem(array $words, string $language = 'en'): array
    {
        $stemmed = [];
        
        foreach ($words as $word) {
            $stemmed[] = $this->porterStem(mb_strtolower($word));
        }

        return [
            'original' => $words,
            'stemmed' => $stemmed,
            'unique_original' => count(array_unique($words)),
            'unique_stemmed' => count(array_unique($stemmed)),
            'language' => $language
        ];
    }

    /**
     * Basic lemmatization (simplified)
     */
    private function lemmatize(array $words, string $language = 'en'): array
    {
        // Basic lemmatization rules for English
        $lemmaRules = [
            'running' => 'run', 'ran' => 'run', 'runs' => 'run',
            'better' => 'good', 'best' => 'good',
            'worse' => 'bad', 'worst' => 'bad',
            'children' => 'child', 'men' => 'man', 'women' => 'woman',
            'feet' => 'foot', 'teeth' => 'tooth'
        ];

        $lemmatized = [];
        
        foreach ($words as $word) {
            $lower = mb_strtolower($word);
            $lemmatized[] = $lemmaRules[$lower] ?? $this->porterStem($lower);
        }

        return [
            'original' => $words,
            'lemmatized' => $lemmatized,
            'unique_original' => count(array_unique($words)),
            'unique_lemmatized' => count(array_unique($lemmatized)),
            'language' => $language
        ];
    }

    /**
     * Generate N-grams
     */
    private function generateNGrams(string $text, int $n = 2): array
    {
        if ($n < 1) {
            throw new InvalidArgumentException('N must be greater than 0');
        }

        $words = $this->tokenize($text, 'word')['tokens'];
        $ngrams = [];
        
        for ($i = 0; $i <= count($words) - $n; $i++) {
            $ngram = array_slice($words, $i, $n);
            $ngramString = implode(' ', $ngram);
            
            if (!isset($ngrams[$ngramString])) {
                $ngrams[$ngramString] = 0;
            }
            $ngrams[$ngramString]++;
        }

        arsort($ngrams);

        return [
            'n' => $n,
            'ngrams' => $ngrams,
            'total_ngrams' => count($ngrams),
            'most_common' => array_slice($ngrams, 0, 10, true)
        ];
    }

    /**
     * Calculate word frequency
     */
    private function wordFrequency(string $text, ?int $limit = null): array
    {
        $words = array_map('strtolower', $this->tokenize($text, 'word')['tokens']);
        $frequency = array_count_values($words);
        arsort($frequency);
        
        if ($limit !== null) {
            $frequency = array_slice($frequency, 0, $limit, true);
        }

        return [
            'frequency' => $frequency,
            'total_words' => count($words),
            'unique_words' => count($frequency),
            'most_common' => array_slice($frequency, 0, 10, true)
        ];
    }

    /**
     * Calculate readability scores
     */
    private function readabilityScore(string $text): array
    {
        if (empty($text)) {
            return ['flesch_kincaid' => 0, 'reading_level' => 'unknown'];
        }

        $sentences = $this->splitIntoSentences($text);
        $words = $this->tokenize($text, 'word')['tokens'];
        $syllables = $this->countTotalSyllables($words);

        $sentenceCount = count($sentences);
        $wordCount = count($words);
        $syllableCount = $syllables;

        // Flesch-Kincaid Grade Level
        $fleschKincaid = 0;
        if ($sentenceCount > 0 && $wordCount > 0) {
            $fleschKincaid = 0.39 * ($wordCount / $sentenceCount) + 11.8 * ($syllableCount / $wordCount) - 15.59;
        }

        // Flesch Reading Ease
        $fleschEase = 206.835 - 1.015 * ($wordCount / $sentenceCount) - 84.6 * ($syllableCount / $wordCount);
        
        $readingLevel = $this->getReadingLevel($fleschKincaid);

        return [
            'flesch_kincaid_grade' => round($fleschKincaid, 2),
            'flesch_reading_ease' => round($fleschEase, 2),
            'reading_level' => $readingLevel,
            'word_count' => $wordCount,
            'sentence_count' => $sentenceCount,
            'syllable_count' => $syllableCount,
            'avg_words_per_sentence' => $sentenceCount > 0 ? round($wordCount / $sentenceCount, 2) : 0,
            'avg_syllables_per_word' => $wordCount > 0 ? round($syllableCount / $wordCount, 2) : 0
        ];
    }

    /**
     * Basic language detection
     */
    private function detectLanguage(string $text): array
    {
        // Simple language detection based on common words
        $languageWords = [
            'en' => ['the', 'and', 'or', 'but', 'in', 'on', 'at', 'to', 'for', 'of', 'with', 'by', 'is', 'are', 'was', 'were'],
            'es' => ['el', 'la', 'de', 'que', 'y', 'en', 'un', 'es', 'se', 'no', 'te', 'lo', 'le', 'da', 'su', 'por'],
            'fr' => ['le', 'de', 'et', 'à', 'un', 'il', 'être', 'et', 'en', 'avoir', 'que', 'pour', 'dans', 'ce', 'son', 'une'],
            'de' => ['der', 'die', 'und', 'in', 'den', 'von', 'zu', 'das', 'mit', 'sich', 'des', 'auf', 'für', 'ist', 'im', 'dem']
        ];

        $words = array_map('strtolower', $this->tokenize($text, 'word')['tokens']);
        $scores = [];

        foreach ($languageWords as $lang => $commonWords) {
            $score = 0;
            foreach ($words as $word) {
                if (in_array($word, $commonWords)) {
                    $score++;
                }
            }
            $scores[$lang] = count($words) > 0 ? $score / count($words) : 0;
        }

        arsort($scores);
        $detectedLang = array_key_first($scores);
        $confidence = $scores[$detectedLang];

        return [
            'detected_language' => $detectedLang,
            'confidence' => round($confidence, 4),
            'scores' => $scores,
            'method' => 'common_words'
        ];
    }

    /**
     * Basic text summarization
     */
    private function summarizeText(string $text, int $sentences = 3): array
    {
        $originalSentences = $this->splitIntoSentences($text);
        
        if (count($originalSentences) <= $sentences) {
            return [
                'summary' => $text,
                'original_sentences' => count($originalSentences),
                'summary_sentences' => count($originalSentences),
                'compression_ratio' => 1.0
            ];
        }

        // Score sentences based on word frequency
        $wordFreq = $this->wordFrequency($text)['frequency'];
        $sentenceScores = [];

        foreach ($originalSentences as $i => $sentence) {
            $words = array_map('strtolower', $this->tokenize($sentence, 'word')['tokens']);
            $score = 0;
            
            foreach ($words as $word) {
                if (isset($wordFreq[$word])) {
                    $score += $wordFreq[$word];
                }
            }
            
            $sentenceScores[$i] = count($words) > 0 ? $score / count($words) : 0;
        }

        // Select top sentences
        arsort($sentenceScores);
        $topIndices = array_slice(array_keys($sentenceScores), 0, $sentences);
        sort($topIndices); // Maintain original order

        $summaryText = '';
        foreach ($topIndices as $index) {
            $summaryText .= $originalSentences[$index] . ' ';
        }

        return [
            'summary' => trim($summaryText),
            'original_sentences' => count($originalSentences),
            'summary_sentences' => $sentences,
            'compression_ratio' => round($sentences / count($originalSentences), 4),
            'selected_sentence_indices' => $topIndices
        ];
    }

    /**
     * Extract named entities (basic implementation)
     */
    private function extractEntities(string $text): array
    {
        $entities = [
            'persons' => [],
            'organizations' => [],
            'locations' => [],
            'dates' => [],
            'emails' => [],
            'urls' => []
        ];

        // Extract emails
        if (preg_match_all('/\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/', $text, $matches)) {
            $entities['emails'] = array_unique($matches[0]);
        }

        // Extract URLs
        if (preg_match_all('/https?:\/\/[^\s]+/', $text, $matches)) {
            $entities['urls'] = array_unique($matches[0]);
        }

        // Extract dates (simple patterns)
        $datePatterns = [
            '/\b\d{1,2}\/\d{1,2}\/\d{4}\b/',
            '/\b\d{4}-\d{2}-\d{2}\b/',
            '/\b(January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2},?\s+\d{4}\b/i'
        ];
        
        foreach ($datePatterns as $pattern) {
            if (preg_match_all($pattern, $text, $matches)) {
                $entities['dates'] = array_merge($entities['dates'], $matches[0]);
            }
        }
        $entities['dates'] = array_unique($entities['dates']);

        // Extract potential proper nouns (capitalized words)
        if (preg_match_all('/\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b/', $text, $matches)) {
            $properNouns = array_unique($matches[0]);
            
            // Simple classification (very basic)
            foreach ($properNouns as $noun) {
                if (str_word_count($noun) >= 2) {
                    $entities['persons'][] = $noun; // Assume multi-word proper nouns are persons
                } else {
                    $entities['locations'][] = $noun; // Single word could be location
                }
            }
        }

        return [
            'entities' => $entities,
            'total_entities' => array_sum(array_map('count', $entities)),
            'entity_types' => array_keys(array_filter($entities, fn($e) => !empty($e)))
        ];
    }

    // Helper methods

    private function initializeWordLists(): void
    {
        $this->stopWords = [
            'en' => [
                'a', 'an', 'and', 'are', 'as', 'at', 'be', 'by', 'for', 'from', 'has',
                'he', 'in', 'is', 'it', 'its', 'of', 'on', 'that', 'the', 'to', 'was',
                'will', 'with', 'the', 'this', 'but', 'they', 'have', 'had', 'what',
                'said', 'each', 'which', 'she', 'do', 'how', 'their', 'if', 'up', 'out',
                'many', 'then', 'them', 'these', 'so', 'some', 'her', 'would', 'make',
                'like', 'into', 'him', 'has', 'two', 'more', 'go', 'no', 'way', 'could',
                'my', 'than', 'first', 'been', 'call', 'who', 'oil', 'sit', 'now', 'find',
                'down', 'day', 'did', 'get', 'come', 'made', 'may', 'part'
            ]
        ];

        $this->commonWords = [
            'en' => ['time', 'person', 'year', 'way', 'day', 'thing', 'man', 'world', 'life', 'hand']
        ];
    }

    private function getEmptyAnalysis(): array
    {
        return [
            'basic_stats' => ['characters' => 0, 'words' => 0, 'sentences' => 0, 'paragraphs' => 0],
            'averages' => ['words_per_sentence' => 0, 'chars_per_word' => 0],
            'complexity' => ['score' => 0, 'level' => 'none'],
            'readability' => ['flesch_kincaid_grade' => 0, 'reading_level' => 'unknown'],
            'sentiment' => ['sentiment' => 'neutral', 'score' => 0]
        ];
    }

    private function splitIntoSentences(string $text): array
    {
        $sentences = preg_split('/[.!?]+/', $text, -1, PREG_SPLIT_NO_EMPTY);
        return array_map('trim', $sentences);
    }

    private function textComplexity(string $text, array $words, array $sentences): array
    {
        $avgWordsPerSentence = count($sentences) > 0 ? count($words) / count($sentences) : 0;
        $longWords = count(array_filter($words, fn($word) => mb_strlen($word) > 6));
        $complexityScore = $avgWordsPerSentence + ($longWords / count($words)) * 100;

        $level = 'simple';
        if ($complexityScore > 20) $level = 'complex';
        elseif ($complexityScore > 15) $level = 'moderate';
        elseif ($complexityScore > 10) $level = 'easy';

        return [
            'score' => round($complexityScore, 2),
            'level' => $level,
            'avg_words_per_sentence' => round($avgWordsPerSentence, 2),
            'long_words' => $longWords,
            'long_word_percentage' => count($words) > 0 ? round(($longWords / count($words)) * 100, 2) : 0
        ];
    }

    private function analyzeLanguageFeatures(string $text): array
    {
        return [
            'exclamations' => substr_count($text, '!'),
            'questions' => substr_count($text, '?'),
            'quotes' => substr_count($text, '"') + substr_count($text, "'"),
            'parentheses' => substr_count($text, '('),
            'uppercase_words' => preg_match_all('/\b[A-Z]{2,}\b/', $text),
            'numbers' => preg_match_all('/\b\d+\b/', $text)
        ];
    }

    private function porterStem(string $word): string
    {
        // Very simplified Porter stemmer
        $word = mb_strtolower($word);
        
        // Step 1a
        if (str_ends_with($word, 'sses')) {
            $word = substr($word, 0, -2);
        } elseif (str_ends_with($word, 'ies')) {
            $word = substr($word, 0, -2);
        } elseif (str_ends_with($word, 'ss')) {
            // Keep as is
        } elseif (str_ends_with($word, 's')) {
            $word = substr($word, 0, -1);
        }

        // Step 1b (simplified)
        if (str_ends_with($word, 'ing')) {
            $word = substr($word, 0, -3);
        } elseif (str_ends_with($word, 'ed')) {
            $word = substr($word, 0, -2);
        }

        return $word;
    }

    private function countSyllables(string $word): int
    {
        $word = mb_strtolower(preg_replace('/[^a-zA-Z]/', '', $word));
        if (strlen($word) <= 3) return 1;
        
        $vowels = 'aeiouy';
        $syllables = 0;
        $prevWasVowel = false;
        
        for ($i = 0; $i < strlen($word); $i++) {
            $isVowel = strpos($vowels, $word[$i]) !== false;
            if ($isVowel && !$prevWasVowel) {
                $syllables++;
            }
            $prevWasVowel = $isVowel;
        }
        
        // Adjust for silent e
        if (str_ends_with($word, 'e')) {
            $syllables--;
        }
        
        return max(1, $syllables);
    }

    private function countTotalSyllables(array $words): int
    {
        $total = 0;
        foreach ($words as $word) {
            $total += $this->countSyllables($word);
        }
        return $total;
    }

    private function getReadingLevel(float $grade): string
    {
        if ($grade < 6) return 'elementary';
        if ($grade < 9) return 'middle_school';
        if ($grade < 13) return 'high_school';
        if ($grade < 16) return 'college';
        return 'graduate';
    }
} 