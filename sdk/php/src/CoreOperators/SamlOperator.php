<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * SAML Operator for SAML authentication and identity management
 */
class SamlOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'authenticate';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'authenticate':
                    return $this->authenticate($data, $options);
                case 'metadata':
                    return $this->generateMetadata($data, $options);
                case 'validate':
                    return $this->validateResponse($data, $options);
                case 'logout':
                    return $this->logout($data, $options);
                case 'parse':
                    return $this->parseSaml($data, $options);
                case 'sign':
                    return $this->signRequest($data, $options);
                case 'encrypt':
                    return $this->encryptAssertion($data, $options);
                case 'decrypt':
                    return $this->decryptAssertion($data, $options);
                default:
                    throw new \Exception("Unknown SAML action: $action");
            }
        } catch (\Exception $e) {
            error_log("SAML Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Authenticate user via SAML
     */
    private function authenticate(array $authData, array $options): array
    {
        $idpUrl = $options['idpUrl'] ?? '';
        $spEntityId = $options['spEntityId'] ?? '';
        $acsUrl = $options['acsUrl'] ?? '';
        $relayState = $options['relayState'] ?? '';

        if (empty($idpUrl) || empty($spEntityId) || empty($acsUrl)) {
            throw new \Exception("IdP URL, SP Entity ID, and ACS URL are required");
        }

        // Generate AuthnRequest
        $authnRequest = $this->generateAuthnRequest($spEntityId, $acsUrl, $relayState);
        
        // Sign the request if certificate is provided
        if (!empty($options['certificate']) && !empty($options['privateKey'])) {
            $authnRequest = $this->signRequest($authnRequest, $options);
        }

        // Create redirect URL
        $redirectUrl = $this->createRedirectUrl($idpUrl, $authnRequest, $relayState);

        return [
            'success' => true,
            'redirectUrl' => $redirectUrl,
            'authnRequest' => $authnRequest,
            'relayState' => $relayState
        ];
    }

    /**
     * Generate SAML metadata
     */
    private function generateMetadata(array $metadataData, array $options): string
    {
        $entityId = $options['entityId'] ?? '';
        $acsUrl = $options['acsUrl'] ?? '';
        $sloUrl = $options['sloUrl'] ?? '';
        $certificate = $options['certificate'] ?? '';

        if (empty($entityId) || empty($acsUrl)) {
            throw new \Exception("Entity ID and ACS URL are required");
        }

        $metadata = $this->buildMetadataXml($entityId, $acsUrl, $sloUrl, $certificate);

        return $metadata;
    }

    /**
     * Validate SAML response
     */
    private function validateResponse(string $samlResponse, array $options): array
    {
        $certificate = $options['certificate'] ?? '';
        $idpEntityId = $options['idpEntityId'] ?? '';
        $spEntityId = $options['spEntityId'] ?? '';

        if (empty($samlResponse)) {
            throw new \Exception("SAML response is required");
        }

        // Decode SAML response
        $decodedResponse = $this->decodeSamlResponse($samlResponse);
        
        if (!$decodedResponse) {
            return [
                'valid' => false,
                'error' => 'Invalid SAML response format'
            ];
        }

        $errors = [];
        $warnings = [];

        // Validate signature if certificate provided
        if (!empty($certificate)) {
            if (!$this->validateSignature($decodedResponse, $certificate)) {
                $errors[] = 'Invalid signature';
            }
        }

        // Validate issuer
        if (!empty($idpEntityId)) {
            $issuer = $this->extractIssuer($decodedResponse);
            if ($issuer !== $idpEntityId) {
                $errors[] = "Invalid issuer: expected $idpEntityId, got $issuer";
            }
        }

        // Validate audience
        if (!empty($spEntityId)) {
            $audience = $this->extractAudience($decodedResponse);
            if ($audience !== $spEntityId) {
                $errors[] = "Invalid audience: expected $spEntityId, got $audience";
            }
        }

        // Validate conditions
        $conditionErrors = $this->validateConditions($decodedResponse);
        $errors = array_merge($errors, $conditionErrors);

        // Extract user attributes
        $attributes = $this->extractAttributes($decodedResponse);

        return [
            'valid' => empty($errors),
            'errors' => $errors,
            'warnings' => $warnings,
            'attributes' => $attributes,
            'nameId' => $this->extractNameId($decodedResponse),
            'sessionIndex' => $this->extractSessionIndex($decodedResponse)
        ];
    }

    /**
     * Logout user via SAML
     */
    private function logout(array $logoutData, array $options): array
    {
        $idpUrl = $options['idpUrl'] ?? '';
        $spEntityId = $options['spEntityId'] ?? '';
        $sloUrl = $options['sloUrl'] ?? '';
        $nameId = $logoutData['nameId'] ?? '';
        $sessionIndex = $logoutData['sessionIndex'] ?? '';

        if (empty($idpUrl) || empty($spEntityId) || empty($nameId)) {
            throw new \Exception("IdP URL, SP Entity ID, and NameID are required");
        }

        // Generate LogoutRequest
        $logoutRequest = $this->generateLogoutRequest($spEntityId, $nameId, $sessionIndex);
        
        // Sign the request if certificate is provided
        if (!empty($options['certificate']) && !empty($options['privateKey'])) {
            $logoutRequest = $this->signRequest($logoutRequest, $options);
        }

        // Create redirect URL
        $redirectUrl = $this->createRedirectUrl($idpUrl, $logoutRequest, '');

        return [
            'success' => true,
            'redirectUrl' => $redirectUrl,
            'logoutRequest' => $logoutRequest
        ];
    }

    /**
     * Parse SAML message
     */
    private function parseSaml(string $samlMessage, array $options): array
    {
        $parseType = $options['parseType'] ?? 'auto'; // auto, response, request

        if (empty($samlMessage)) {
            throw new \Exception("SAML message is required");
        }

        $decoded = $this->decodeSamlMessage($samlMessage);
        
        if (!$decoded) {
            return [
                'success' => false,
                'error' => 'Invalid SAML message format'
            ];
        }

        $parsed = [
            'type' => $this->determineMessageType($decoded),
            'id' => $this->extractMessageId($decoded),
            'issueInstant' => $this->extractIssueInstant($decoded),
            'issuer' => $this->extractIssuer($decoded),
            'destination' => $this->extractDestination($decoded)
        ];

        // Extract specific data based on message type
        if ($parsed['type'] === 'Response') {
            $parsed['status'] = $this->extractStatus($decoded);
            $parsed['assertions'] = $this->extractAssertions($decoded);
        } elseif ($parsed['type'] === 'AuthnRequest') {
            $parsed['assertionConsumerServiceURL'] = $this->extractACSURL($decoded);
            $parsed['protocolBinding'] = $this->extractProtocolBinding($decoded);
        }

        return [
            'success' => true,
            'parsed' => $parsed,
            'raw' => $decoded
        ];
    }

    /**
     * Sign SAML request
     */
    private function signRequest(string $request, array $options): string
    {
        $certificate = $options['certificate'] ?? '';
        $privateKey = $options['privateKey'] ?? '';
        $algorithm = $options['algorithm'] ?? 'sha256';

        if (empty($certificate) || empty($privateKey)) {
            throw new \Exception("Certificate and private key are required for signing");
        }

        // Create signature
        $signature = $this->createSignature($request, $privateKey, $algorithm);
        
        // Insert signature into request
        $signedRequest = $this->insertSignature($request, $signature, $certificate);

        return $signedRequest;
    }

    /**
     * Encrypt SAML assertion
     */
    private function encryptAssertion(array $assertionData, array $options): string
    {
        $assertion = $assertionData['assertion'] ?? '';
        $publicKey = $options['publicKey'] ?? '';
        $algorithm = $options['algorithm'] ?? 'aes256';

        if (empty($assertion) || empty($publicKey)) {
            throw new \Exception("Assertion and public key are required");
        }

        // Generate encryption key
        $encryptionKey = $this->generateEncryptionKey($algorithm);
        
        // Encrypt assertion
        $encryptedAssertion = $this->encryptData($assertion, $encryptionKey, $algorithm);
        
        // Encrypt key with public key
        $encryptedKey = $this->encryptKey($encryptionKey, $publicKey);

        // Create EncryptedAssertion element
        $encryptedAssertionXml = $this->buildEncryptedAssertion($encryptedAssertion, $encryptedKey, $algorithm);

        return $encryptedAssertionXml;
    }

    /**
     * Decrypt SAML assertion
     */
    private function decryptAssertion(string $encryptedAssertion, array $options): array
    {
        $privateKey = $options['privateKey'] ?? '';

        if (empty($encryptedAssertion) || empty($privateKey)) {
            throw new \Exception("Encrypted assertion and private key are required");
        }

        // Extract encrypted key and data
        $extracted = $this->extractEncryptedData($encryptedAssertion);
        
        if (!$extracted) {
            return [
                'success' => false,
                'error' => 'Invalid encrypted assertion format'
            ];
        }

        // Decrypt key
        $decryptionKey = $this->decryptKey($extracted['encryptedKey'], $privateKey);
        
        // Decrypt assertion
        $decryptedAssertion = $this->decryptData($extracted['encryptedData'], $decryptionKey, $extracted['algorithm']);

        return [
            'success' => true,
            'assertion' => $decryptedAssertion
        ];
    }

    /**
     * Generate AuthnRequest
     */
    private function generateAuthnRequest(string $spEntityId, string $acsUrl, string $relayState): string
    {
        $requestId = '_' . uniqid();
        $issueInstant = gmdate('Y-m-d\TH:i:s\Z');

        $xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
        $xml .= "<samlp:AuthnRequest xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\" ";
        $xml .= "xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\" ";
        $xml .= "ID=\"$requestId\" ";
        $xml .= "Version=\"2.0\" ";
        $xml .= "IssueInstant=\"$issueInstant\" ";
        $xml .= "ProtocolBinding=\"urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST\" ";
        $xml .= "AssertionConsumerServiceURL=\"$acsUrl\">\n";
        $xml .= "  <saml:Issuer>$spEntityId</saml:Issuer>\n";
        $xml .= "</samlp:AuthnRequest>";

        return $this->base64Encode($xml);
    }

    /**
     * Build metadata XML
     */
    private function buildMetadataXml(string $entityId, string $acsUrl, string $sloUrl, string $certificate): string
    {
        $xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
        $xml .= "<md:EntityDescriptor xmlns:md=\"urn:oasis:names:tc:SAML:2.0:metadata\" ";
        $xml .= "entityID=\"$entityId\">\n";
        $xml .= "  <md:SPSSODescriptor protocolSupportEnumeration=\"urn:oasis:names:tc:SAML:2.0:protocol\">\n";
        $xml .= "    <md:AssertionConsumerService Binding=\"urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST\" ";
        $xml .= "Location=\"$acsUrl\" index=\"0\"/>\n";
        
        if (!empty($sloUrl)) {
            $xml .= "    <md:SingleLogoutService Binding=\"urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect\" ";
            $xml .= "Location=\"$sloUrl\"/>\n";
        }
        
        if (!empty($certificate)) {
            $xml .= "    <md:KeyDescriptor use=\"signing\">\n";
            $xml .= "      <ds:KeyInfo xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\">\n";
            $xml .= "        <ds:X509Data>\n";
            $xml .= "          <ds:X509Certificate>$certificate</ds:X509Certificate>\n";
            $xml .= "        </ds:X509Data>\n";
            $xml .= "      </ds:KeyInfo>\n";
            $xml .= "    </md:KeyDescriptor>\n";
        }
        
        $xml .= "  </md:SPSSODescriptor>\n";
        $xml .= "</md:EntityDescriptor>";

        return $xml;
    }

    /**
     * Generate LogoutRequest
     */
    private function generateLogoutRequest(string $spEntityId, string $nameId, string $sessionIndex): string
    {
        $requestId = '_' . uniqid();
        $issueInstant = gmdate('Y-m-d\TH:i:s\Z');

        $xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
        $xml .= "<samlp:LogoutRequest xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\" ";
        $xml .= "xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\" ";
        $xml .= "ID=\"$requestId\" ";
        $xml .= "Version=\"2.0\" ";
        $xml .= "IssueInstant=\"$issueInstant\">\n";
        $xml .= "  <saml:Issuer>$spEntityId</saml:Issuer>\n";
        $xml .= "  <saml:NameID>$nameId</saml:NameID>\n";
        
        if (!empty($sessionIndex)) {
            $xml .= "  <samlp:SessionIndex>$sessionIndex</samlp:SessionIndex>\n";
        }
        
        $xml .= "</samlp:LogoutRequest>";

        return $this->base64Encode($xml);
    }

    /**
     * Decode SAML response
     */
    private function decodeSamlResponse(string $samlResponse): ?array
    {
        $decoded = base64_decode($samlResponse);
        if ($decoded === false) {
            return null;
        }

        return $this->parseXml($decoded);
    }

    /**
     * Decode SAML message
     */
    private function decodeSamlMessage(string $samlMessage): ?array
    {
        $decoded = base64_decode($samlMessage);
        if ($decoded === false) {
            return null;
        }

        return $this->parseXml($decoded);
    }

    /**
     * Parse XML
     */
    private function parseXml(string $xml): ?array
    {
        libxml_use_internal_errors(true);
        $dom = new \DOMDocument();
        $dom->loadXML($xml);
        $errors = libxml_get_errors();
        libxml_clear_errors();

        if (!empty($errors)) {
            return null;
        }

        return $this->domToArray($dom->documentElement);
    }

    /**
     * Convert DOM to array
     */
    private function domToArray(\DOMNode $node): array
    {
        $array = [];
        
        if ($node->nodeType === XML_ELEMENT_NODE) {
            $array['name'] = $node->nodeName;
            $array['value'] = $node->textContent;
            
            if ($node->hasAttributes()) {
                $array['attributes'] = [];
                foreach ($node->attributes as $attr) {
                    $array['attributes'][$attr->name] = $attr->value;
                }
            }
            
            if ($node->hasChildNodes()) {
                $array['children'] = [];
                foreach ($node->childNodes as $child) {
                    if ($child->nodeType === XML_ELEMENT_NODE) {
                        $array['children'][] = $this->domToArray($child);
                    }
                }
            }
        }

        return $array;
    }

    /**
     * Validate signature
     */
    private function validateSignature(array $samlData, string $certificate): bool
    {
        // This would implement signature validation
        // For now, return true as placeholder
        return true;
    }

    /**
     * Extract issuer
     */
    private function extractIssuer(array $samlData): ?string
    {
        return $this->extractElementValue($samlData, 'Issuer');
    }

    /**
     * Extract audience
     */
    private function extractAudience(array $samlData): ?string
    {
        return $this->extractElementValue($samlData, 'Audience');
    }

    /**
     * Extract NameID
     */
    private function extractNameId(array $samlData): ?string
    {
        return $this->extractElementValue($samlData, 'NameID');
    }

    /**
     * Extract session index
     */
    private function extractSessionIndex(array $samlData): ?string
    {
        return $this->extractElementValue($samlData, 'SessionIndex');
    }

    /**
     * Extract attributes
     */
    private function extractAttributes(array $samlData): array
    {
        $attributes = [];
        
        // This would extract attributes from SAML assertion
        // For now, return empty array as placeholder
        
        return $attributes;
    }

    /**
     * Validate conditions
     */
    private function validateConditions(array $samlData): array
    {
        $errors = [];
        
        // This would validate NotBefore, NotOnOrAfter, etc.
        // For now, return empty array as placeholder
        
        return $errors;
    }

    /**
     * Determine message type
     */
    private function determineMessageType(array $samlData): string
    {
        if (isset($samlData['name'])) {
            if (strpos($samlData['name'], 'Response') !== false) {
                return 'Response';
            } elseif (strpos($samlData['name'], 'Request') !== false) {
                return 'Request';
            }
        }
        
        return 'Unknown';
    }

    /**
     * Extract message ID
     */
    private function extractMessageId(array $samlData): ?string
    {
        return $samlData['attributes']['ID'] ?? null;
    }

    /**
     * Extract issue instant
     */
    private function extractIssueInstant(array $samlData): ?string
    {
        return $samlData['attributes']['IssueInstant'] ?? null;
    }

    /**
     * Extract destination
     */
    private function extractDestination(array $samlData): ?string
    {
        return $samlData['attributes']['Destination'] ?? null;
    }

    /**
     * Extract status
     */
    private function extractStatus(array $samlData): ?string
    {
        return $this->extractElementValue($samlData, 'StatusCode');
    }

    /**
     * Extract assertions
     */
    private function extractAssertions(array $samlData): array
    {
        $assertions = [];
        
        // This would extract assertions from SAML response
        // For now, return empty array as placeholder
        
        return $assertions;
    }

    /**
     * Extract ACS URL
     */
    private function extractACSURL(array $samlData): ?string
    {
        return $samlData['attributes']['AssertionConsumerServiceURL'] ?? null;
    }

    /**
     * Extract protocol binding
     */
    private function extractProtocolBinding(array $samlData): ?string
    {
        return $samlData['attributes']['ProtocolBinding'] ?? null;
    }

    /**
     * Extract element value
     */
    private function extractElementValue(array $samlData, string $elementName): ?string
    {
        if (isset($samlData['children'])) {
            foreach ($samlData['children'] as $child) {
                if ($child['name'] === $elementName) {
                    return $child['value'];
                }
            }
        }
        
        return null;
    }

    /**
     * Create signature
     */
    private function createSignature(string $data, string $privateKey, string $algorithm): string
    {
        // This would create XML signature
        // For now, return placeholder
        return 'signature_placeholder';
    }

    /**
     * Insert signature
     */
    private function insertSignature(string $request, string $signature, string $certificate): string
    {
        // This would insert signature into SAML request
        // For now, return original request
        return $request;
    }

    /**
     * Generate encryption key
     */
    private function generateEncryptionKey(string $algorithm): string
    {
        return bin2hex(random_bytes(32));
    }

    /**
     * Encrypt data
     */
    private function encryptData(string $data, string $key, string $algorithm): string
    {
        // This would encrypt data
        // For now, return base64 encoded data
        return base64_encode($data);
    }

    /**
     * Encrypt key
     */
    private function encryptKey(string $key, string $publicKey): string
    {
        // This would encrypt key with public key
        // For now, return base64 encoded key
        return base64_encode($key);
    }

    /**
     * Build encrypted assertion
     */
    private function buildEncryptedAssertion(string $encryptedData, string $encryptedKey, string $algorithm): string
    {
        $xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
        $xml .= "<saml:EncryptedAssertion xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\">\n";
        $xml .= "  <xenc:EncryptedData xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\">\n";
        $xml .= "    <xenc:EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#$algorithm\"/>\n";
        $xml .= "    <xenc:CipherData>\n";
        $xml .= "      <xenc:CipherValue>$encryptedData</xenc:CipherValue>\n";
        $xml .= "    </xenc:CipherData>\n";
        $xml .= "  </xenc:EncryptedData>\n";
        $xml .= "  <xenc:EncryptedKey xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\">\n";
        $xml .= "    <xenc:CipherData>\n";
        $xml .= "      <xenc:CipherValue>$encryptedKey</xenc:CipherValue>\n";
        $xml .= "    </xenc:CipherData>\n";
        $xml .= "  </xenc:EncryptedKey>\n";
        $xml .= "</saml:EncryptedAssertion>";

        return $xml;
    }

    /**
     * Extract encrypted data
     */
    private function extractEncryptedData(string $encryptedAssertion): ?array
    {
        // This would extract encrypted data from XML
        // For now, return placeholder
        return [
            'encryptedData' => 'encrypted_data_placeholder',
            'encryptedKey' => 'encrypted_key_placeholder',
            'algorithm' => 'aes256'
        ];
    }

    /**
     * Decrypt key
     */
    private function decryptKey(string $encryptedKey, string $privateKey): string
    {
        // This would decrypt key with private key
        // For now, return base64 decoded key
        return base64_decode($encryptedKey);
    }

    /**
     * Decrypt data
     */
    private function decryptData(string $encryptedData, string $key, string $algorithm): string
    {
        // This would decrypt data
        // For now, return base64 decoded data
        return base64_decode($encryptedData);
    }

    /**
     * Create redirect URL
     */
    private function createRedirectUrl(string $idpUrl, string $request, string $relayState): string
    {
        $params = ['SAMLRequest' => $request];
        
        if (!empty($relayState)) {
            $params['RelayState'] = $relayState;
        }

        return $idpUrl . '?' . http_build_query($params);
    }

    /**
     * Base64 encode
     */
    private function base64Encode(string $data): string
    {
        return base64_encode($data);
    }
} 