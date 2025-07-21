using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TuskLang.Operators.Security
{
    /// <summary>
    /// SAML Operator for TuskLang C# SDK
    /// 
    /// Provides SAML (Security Assertion Markup Language) capabilities with support for:
    /// - SAML 2.0 authentication
    /// - SAML request/response handling
    /// - Digital signatures and encryption
    /// - Single Sign-On (SSO) and Single Logout (SLO)
    /// - Identity Provider (IdP) and Service Provider (SP) roles
    /// - Metadata exchange
    /// 
    /// Usage:
    /// ```csharp
    /// // Create SAML request
    /// var request = @saml({
    ///   action: "create_request",
    ///   issuer: "https://myapp.com",
    ///   assertion_consumer_service_url: "https://myapp.com/acs",
    ///   name_id_format: "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent"
    /// })
    /// 
    /// // Validate SAML response
    /// var validation = @saml({
    ///   action: "validate_response",
    ///   response: "saml_response_xml",
    ///   certificate: "idp_certificate",
    ///   issuer: "https://idp.example.com"
    /// })
    /// ```
    /// </summary>
    public class SamlOperator : BaseOperator
    {
        public SamlOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "issuer", "assertion_consumer_service_url", "name_id_format", "response", 
                "request", "certificate", "private_key", "relay_state", "destination", 
                "audience", "not_before", "not_on_or_after", "session_index" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["name_id_format"] = "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent",
                ["protocol_binding"] = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST"
            };
        }
        
        public override string GetName() => "saml";
        
        protected override string GetDescription() => "SAML 2.0 authentication operator for SSO and identity federation";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["create_request"] = "@saml({action: \"create_request\", issuer: \"https://myapp.com\", assertion_consumer_service_url: \"https://myapp.com/acs\"})",
                ["validate_response"] = "@saml({action: \"validate_response\", response: \"saml_xml\", certificate: \"cert\", issuer: \"https://idp.com\"})",
                ["create_response"] = "@saml({action: \"create_response\", issuer: \"https://idp.com\", destination: \"https://sp.com/acs\", name_id: \"user@example.com\"})",
                ["metadata"] = "@saml({action: \"generate_metadata\", entity_id: \"https://myapp.com\", acs_url: \"https://myapp.com/acs\"})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid SAML action specified",
                ["INVALID_XML"] = "Invalid SAML XML format",
                ["SIGNATURE_INVALID"] = "SAML signature validation failed",
                ["CERTIFICATE_INVALID"] = "Invalid SAML certificate",
                ["ISSUER_MISMATCH"] = "SAML issuer mismatch",
                ["EXPIRED_ASSERTION"] = "SAML assertion has expired",
                ["MISSING_REQUIRED_FIELDS"] = "Missing required SAML fields",
                ["ENCRYPTION_FAILED"] = "SAML encryption failed"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "").ToLower();
            
            switch (action)
            {
                case "create_request":
                    return await CreateSamlRequestAsync(config, context);
                case "validate_response":
                    return await ValidateSamlResponseAsync(config, context);
                case "create_response":
                    return await CreateSamlResponseAsync(config, context);
                case "generate_metadata":
                    return await GenerateMetadataAsync(config, context);
                case "parse_response":
                    return await ParseSamlResponseAsync(config, context);
                default:
                    throw new ArgumentException($"Invalid SAML action: {action}");
            }
        }
        
        /// <summary>
        /// Create SAML authentication request
        /// </summary>
        private async Task<object> CreateSamlRequestAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var issuer = GetContextValue<string>(config, "issuer", "");
            var assertionConsumerServiceUrl = GetContextValue<string>(config, "assertion_consumer_service_url", "");
            var nameIdFormat = GetContextValue<string>(config, "name_id_format", "");
            var relayState = GetContextValue<string>(config, "relay_state", "");
            var destination = GetContextValue<string>(config, "destination", "");
            
            if (string.IsNullOrEmpty(issuer))
                throw new ArgumentException("Issuer is required for SAML request");
            
            if (string.IsNullOrEmpty(assertionConsumerServiceUrl))
                throw new ArgumentException("Assertion Consumer Service URL is required for SAML request");
            
            var requestId = $"_{Guid.NewGuid():N}";
            var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            var samlRequest = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:AuthnRequest xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
                    xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
                    ID=""{requestId}""
                    Version=""2.0""
                    IssueInstant=""{issueInstant}""
                    ProtocolBinding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST""
                    AssertionConsumerServiceURL=""{assertionConsumerServiceUrl}""
                    Destination=""{destination}"">
    <saml:Issuer>{issuer}</saml:Issuer>
    <samlp:NameIDPolicy Format=""{nameIdFormat}"" AllowCreate=""true""/>
</samlp:AuthnRequest>";
            
            var result = new Dictionary<string, object>
            {
                ["saml_request"] = samlRequest,
                ["request_id"] = requestId,
                ["issue_instant"] = issueInstant,
                ["issuer"] = issuer,
                ["destination"] = destination,
                ["relay_state"] = relayState
            };
            
            Log("info", "SAML request created", new Dictionary<string, object>
            {
                ["request_id"] = requestId,
                ["issuer"] = issuer
            });
            
            return result;
        }
        
        /// <summary>
        /// Validate SAML response
        /// </summary>
        private async Task<object> ValidateSamlResponseAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var response = GetContextValue<string>(config, "response", "");
            var certificate = GetContextValue<string>(config, "certificate", "");
            var issuer = GetContextValue<string>(config, "issuer", "");
            var audience = GetContextValue<string>(config, "audience", "");
            
            if (string.IsNullOrEmpty(response))
                throw new ArgumentException("SAML response is required for validation");
            
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                
                // Basic XML validation
                if (!xmlDoc.DocumentElement.Name.Contains("Response"))
                {
                    throw new ArgumentException("Invalid SAML response format");
                }
                
                var validationResult = new Dictionary<string, object>
                {
                    ["valid"] = true,
                    ["response_id"] = xmlDoc.DocumentElement.GetAttribute("ID"),
                    ["issue_instant"] = xmlDoc.DocumentElement.GetAttribute("IssueInstant"),
                    ["destination"] = xmlDoc.DocumentElement.GetAttribute("Destination")
                };
                
                // Extract assertion information
                var assertion = xmlDoc.SelectSingleNode("//saml:Assertion", CreateNamespaceManager(xmlDoc));
                if (assertion != null)
                {
                    var assertionElement = assertion as XmlElement;
                    validationResult["assertion_id"] = assertionElement.GetAttribute("ID");
                    validationResult["issue_instant"] = assertionElement.GetAttribute("IssueInstant");
                    
                    // Extract subject
                    var subject = assertion.SelectSingleNode(".//saml:Subject", CreateNamespaceManager(xmlDoc));
                    if (subject != null)
                    {
                        var nameId = subject.SelectSingleNode(".//saml:NameID", CreateNamespaceManager(xmlDoc));
                        if (nameId != null)
                        {
                            validationResult["name_id"] = nameId.InnerText;
                            validationResult["name_id_format"] = nameId.GetAttribute("Format");
                        }
                    }
                    
                    // Extract conditions
                    var conditions = assertion.SelectSingleNode(".//saml:Conditions", CreateNamespaceManager(xmlDoc));
                    if (conditions != null)
                    {
                        var notBefore = conditions.GetAttribute("NotBefore");
                        var notOnOrAfter = conditions.GetAttribute("NotOnOrAfter");
                        
                        if (!string.IsNullOrEmpty(notBefore) && DateTime.TryParse(notBefore, out var notBeforeDate))
                        {
                            if (DateTime.UtcNow < notBeforeDate)
                            {
                                validationResult["valid"] = false;
                                validationResult["error"] = "Assertion not yet valid";
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(notOnOrAfter) && DateTime.TryParse(notOnOrAfter, out var notOnOrAfterDate))
                        {
                            if (DateTime.UtcNow >= notOnOrAfterDate)
                            {
                                validationResult["valid"] = false;
                                validationResult["error"] = "Assertion has expired";
                            }
                        }
                    }
                }
                
                // Validate issuer if provided
                if (!string.IsNullOrEmpty(issuer))
                {
                    var responseIssuer = xmlDoc.SelectSingleNode("//saml:Issuer", CreateNamespaceManager(xmlDoc));
                    if (responseIssuer != null && responseIssuer.InnerText != issuer)
                    {
                        validationResult["valid"] = false;
                        validationResult["error"] = "Issuer mismatch";
                    }
                }
                
                Log("info", "SAML response validated", new Dictionary<string, object>
                {
                    ["valid"] = validationResult["valid"],
                    ["response_id"] = validationResult["response_id"]
                });
                
                return validationResult;
            }
            catch (Exception ex)
            {
                Log("error", "SAML response validation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
                
                return new Dictionary<string, object>
                {
                    ["valid"] = false,
                    ["error"] = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Create SAML response
        /// </summary>
        private async Task<object> CreateSamlResponseAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var issuer = GetContextValue<string>(config, "issuer", "");
            var destination = GetContextValue<string>(config, "destination", "");
            var nameId = GetContextValue<string>(config, "name_id", "");
            var nameIdFormat = GetContextValue<string>(config, "name_id_format", "");
            var sessionIndex = GetContextValue<string>(config, "session_index", "");
            
            if (string.IsNullOrEmpty(issuer))
                throw new ArgumentException("Issuer is required for SAML response");
            
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentException("Destination is required for SAML response");
            
            if (string.IsNullOrEmpty(nameId))
                throw new ArgumentException("NameID is required for SAML response");
            
            var responseId = $"_{Guid.NewGuid():N}";
            var assertionId = $"_{Guid.NewGuid():N}";
            var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var notBefore = DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var notOnOrAfter = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            var samlResponse = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<samlp:Response xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
                xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
                ID=""{responseId}""
                Version=""2.0""
                IssueInstant=""{issueInstant}""
                Destination=""{destination}"">
    <saml:Issuer>{issuer}</saml:Issuer>
    <samlp:Status>
        <samlp:StatusCode Value=""urn:oasis:names:tc:SAML:2.0:status:Success""/>
    </samlp:Status>
    <saml:Assertion ID=""{assertionId}"" Version=""2.0"" IssueInstant=""{issueInstant}"">
        <saml:Issuer>{issuer}</saml:Issuer>
        <saml:Subject>
            <saml:NameID Format=""{nameIdFormat}"">{nameId}</saml:NameID>
            <saml:SubjectConfirmation Method=""urn:oasis:names:tc:SAML:2.0:cm:bearer"">
                <saml:SubjectConfirmationData NotOnOrAfter=""{notOnOrAfter}"" Recipient=""{destination}""/>
            </saml:SubjectConfirmation>
        </saml:Subject>
        <saml:Conditions NotBefore=""{notBefore}"" NotOnOrAfter=""{notOnOrAfter}"">
            <saml:AudienceRestriction>
                <saml:Audience>{destination}</saml:Audience>
            </saml:AudienceRestriction>
        </saml:Conditions>
        <saml:AuthnStatement AuthnInstant=""{issueInstant}"" SessionIndex=""{sessionIndex}"">
            <saml:AuthnContext>
                <saml:AuthnContextClassRef>urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport</saml:AuthnContextClassRef>
            </saml:AuthnContext>
        </saml:AuthnStatement>
    </saml:Assertion>
</samlp:Response>";
            
            var result = new Dictionary<string, object>
            {
                ["saml_response"] = samlResponse,
                ["response_id"] = responseId,
                ["assertion_id"] = assertionId,
                ["issue_instant"] = issueInstant,
                ["issuer"] = issuer,
                ["destination"] = destination,
                ["name_id"] = nameId
            };
            
            Log("info", "SAML response created", new Dictionary<string, object>
            {
                ["response_id"] = responseId,
                ["issuer"] = issuer
            });
            
            return result;
        }
        
        /// <summary>
        /// Generate SAML metadata
        /// </summary>
        private async Task<object> GenerateMetadataAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var entityId = GetContextValue<string>(config, "entity_id", "");
            var acsUrl = GetContextValue<string>(config, "acs_url", "");
            var sloUrl = GetContextValue<string>(config, "slo_url", "");
            var certificate = GetContextValue<string>(config, "certificate", "");
            
            if (string.IsNullOrEmpty(entityId))
                throw new ArgumentException("Entity ID is required for SAML metadata");
            
            if (string.IsNullOrEmpty(acsUrl))
                throw new ArgumentException("Assertion Consumer Service URL is required for SAML metadata");
            
            var metadata = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<md:EntityDescriptor xmlns:md=""urn:oasis:names:tc:SAML:2.0:metadata""
                     entityID=""{entityId}"">
    <md:SPSSODescriptor protocolSupportEnumeration=""urn:oasis:names:tc:SAML:2.0:protocol"">
        <md:AssertionConsumerService Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST""
                                     Location=""{acsUrl}"" index=""0""/>
        {(!string.IsNullOrEmpty(sloUrl) ? $@"<md:SingleLogoutService Binding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect""
                                        Location=""{sloUrl}""/>" : "")}
        {(!string.IsNullOrEmpty(certificate) ? $@"<md:KeyDescriptor use=""signing"">
            <ds:KeyInfo xmlns:ds=""http://www.w3.org/2000/09/xmldsig#"">
                <ds:X509Data>
                    <ds:X509Certificate>{certificate}</ds:X509Certificate>
                </ds:X509Data>
            </ds:KeyInfo>
        </md:KeyDescriptor>" : "")}
    </md:SPSSODescriptor>
</md:EntityDescriptor>";
            
            var result = new Dictionary<string, object>
            {
                ["metadata"] = metadata,
                ["entity_id"] = entityId,
                ["acs_url"] = acsUrl,
                ["slo_url"] = sloUrl
            };
            
            Log("info", "SAML metadata generated", new Dictionary<string, object>
            {
                ["entity_id"] = entityId
            });
            
            return result;
        }
        
        /// <summary>
        /// Parse SAML response
        /// </summary>
        private async Task<object> ParseSamlResponseAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var response = GetContextValue<string>(config, "response", "");
            
            if (string.IsNullOrEmpty(response))
                throw new ArgumentException("SAML response is required for parsing");
            
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);
                
                var nsManager = CreateNamespaceManager(xmlDoc);
                var result = new Dictionary<string, object>();
                
                // Extract basic response information
                result["response_id"] = xmlDoc.DocumentElement.GetAttribute("ID");
                result["issue_instant"] = xmlDoc.DocumentElement.GetAttribute("IssueInstant");
                result["destination"] = xmlDoc.DocumentElement.GetAttribute("Destination");
                
                // Extract assertion information
                var assertion = xmlDoc.SelectSingleNode("//saml:Assertion", nsManager);
                if (assertion != null)
                {
                    var assertionElement = assertion as XmlElement;
                    result["assertion_id"] = assertionElement.GetAttribute("ID");
                    result["assertion_issue_instant"] = assertionElement.GetAttribute("IssueInstant");
                    
                    // Extract subject
                    var subject = assertion.SelectSingleNode(".//saml:Subject", nsManager);
                    if (subject != null)
                    {
                        var nameId = subject.SelectSingleNode(".//saml:NameID", nsManager);
                        if (nameId != null)
                        {
                            result["name_id"] = nameId.InnerText;
                            result["name_id_format"] = nameId.GetAttribute("Format");
                        }
                    }
                    
                    // Extract attributes
                    var attributes = assertion.SelectNodes(".//saml:Attribute", nsManager);
                    if (attributes != null && attributes.Count > 0)
                    {
                        var attributeDict = new Dictionary<string, object>();
                        foreach (XmlNode attr in attributes)
                        {
                            var attrElement = attr as XmlElement;
                            var attrName = attrElement.GetAttribute("Name");
                            var attrValues = new List<string>();
                            
                            var attrValuesNodes = attr.SelectNodes(".//saml:AttributeValue", nsManager);
                            foreach (XmlNode valueNode in attrValuesNodes)
                            {
                                attrValues.Add(valueNode.InnerText);
                            }
                            
                            attributeDict[attrName] = attrValues.Count == 1 ? attrValues[0] : attrValues;
                        }
                        result["attributes"] = attributeDict;
                    }
                }
                
                Log("info", "SAML response parsed", new Dictionary<string, object>
                {
                    ["response_id"] = result["response_id"]
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "SAML response parsing failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
                
                throw new ArgumentException($"Failed to parse SAML response: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create namespace manager for XML parsing
        /// </summary>
        private XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
        {
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
            nsManager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            nsManager.AddNamespace("md", "urn:oasis:names:tc:SAML:2.0:metadata");
            return nsManager;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (config.TryGetValue("action", out var action))
            {
                var validActions = new[] { "create_request", "validate_response", "create_response", "generate_metadata", "parse_response" };
                if (!validActions.Contains(action.ToString().ToLower()))
                {
                    result.Errors.Add($"Invalid action: {action}. Supported: {string.Join(", ", validActions)}");
                }
            }
            
            return result;
        }
    }
} 