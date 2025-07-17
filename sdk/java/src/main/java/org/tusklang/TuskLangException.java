package org.tusklang;

/**
 * Exception thrown when TuskLang parsing fails
 */
public class TuskLangException extends Exception {
    
    public TuskLangException(String message) {
        super(message);
    }
    
    public TuskLangException(String message, Throwable cause) {
        super(message, cause);
    }
} 