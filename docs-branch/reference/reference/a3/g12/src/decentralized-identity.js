/**
 * Decentralized Identity and Wallet Management
 * Goal 12.2 Implementation
 */

const crypto = require('crypto');
const EventEmitter = require('events');

class DecentralizedIdentity extends EventEmitter {
    constructor(options = {}) {
        super();
        this.identities = new Map();
        this.wallets = new Map();
        this.credentials = new Map();
        this.verifiers = new Map();
        this.didRegistry = new Map();
        this.defaultMethod = options.defaultMethod || 'did:eth';
        
        this.registerBuiltInVerifiers();
    }

    /**
     * Create decentralized identity (DID)
     */
    createDID(identityId, method = this.defaultMethod) {
        const keyPair = this.generateKeyPair();
        const did = this.generateDID(method, keyPair.publicKey);
        
        const identity = {
            id: identityId,
            did,
            method,
            keyPair,
            createdAt: Date.now(),
            credentials: [],
            verifications: 0,
            metadata: {}
        };

        this.identities.set(identityId, identity);
        this.didRegistry.set(did, identityId);
        
        console.log(`✓ DID created: ${identityId} (${did})`);
        this.emit('didCreated', { identityId, did });
        
        return identity;
    }

    /**
     * Resolve DID to identity
     */
    resolveDID(did) {
        const identityId = this.didRegistry.get(did);
        if (!identityId) {
            throw new Error(`DID ${did} not found`);
        }
        
        return this.identities.get(identityId);
    }

    /**
     * Create verifiable credential
     */
    createCredential(issuerId, subjectId, claims, options = {}) {
        const issuer = this.identities.get(issuerId);
        const subject = this.identities.get(subjectId);
        
        if (!issuer) {
            throw new Error(`Issuer identity ${issuerId} not found`);
        }
        if (!subject) {
            throw new Error(`Subject identity ${subjectId} not found`);
        }

        const credentialId = this.generateCredentialId();
        const credential = {
            id: credentialId,
            issuer: issuer.did,
            subject: subject.did,
            claims,
            issuedAt: Date.now(),
            expiresAt: options.expiresAt || (Date.now() + 365 * 24 * 60 * 60 * 1000), // 1 year
            signature: this.signCredential(claims, issuer.keyPair.privateKey),
            metadata: options.metadata || {}
        };

        this.credentials.set(credentialId, credential);
        subject.credentials.push(credentialId);
        
        console.log(`✓ Credential issued: ${credentialId}`);
        this.emit('credentialIssued', { credentialId, issuer: issuerId, subject: subjectId });
        
        return credential;
    }

    /**
     * Verify credential
     */
    verifyCredential(credentialId, verifierId) {
        const credential = this.credentials.get(credentialId);
        if (!credential) {
            throw new Error(`Credential ${credentialId} not found`);
        }

        const verifier = this.identities.get(verifierId);
        if (!verifier) {
            throw new Error(`Verifier identity ${verifierId} not found`);
        }

        // Check expiration
        if (credential.expiresAt && Date.now() > credential.expiresAt) {
            return { valid: false, reason: 'expired' };
        }

        // Verify signature
        const issuerIdentityId = this.didRegistry.get(credential.issuer);
        const issuer = this.identities.get(issuerIdentityId);
        
        if (!issuer) {
            return { valid: false, reason: 'issuer_not_found' };
        }

        const isValid = this.verifySignature(
            credential.claims,
            credential.signature,
            issuer.keyPair.publicKey
        );

        verifier.verifications++;
        
        this.emit('credentialVerified', { credentialId, verifierId, isValid });
        return { valid: isValid, reason: isValid ? 'valid' : 'invalid_signature' };
    }

    /**
     * Create HD wallet
     */
    createHDWallet(walletId, mnemonic) {
        if (!mnemonic) {
            mnemonic = this.generateMnemonic();
        }

        const masterKey = this.deriveMasterKey(mnemonic);
        const wallet = {
            id: walletId,
            mnemonic,
            masterKey,
            accounts: new Map(),
            createdAt: Date.now(),
            nextAccountIndex: 0
        };

        this.wallets.set(walletId, wallet);
        
        console.log(`✓ HD Wallet created: ${walletId}`);
        this.emit('walletCreated', { walletId });
        
        return { walletId, mnemonic };
    }

    /**
     * Derive account from HD wallet
     */
    deriveAccount(walletId, accountIndex) {
        const wallet = this.wallets.get(walletId);
        if (!wallet) {
            throw new Error(`Wallet ${walletId} not found`);
        }

        if (accountIndex === undefined) {
            accountIndex = wallet.nextAccountIndex++;
        }

        const accountKey = this.deriveAccountKey(wallet.masterKey, accountIndex);
        const address = this.keyToAddress(accountKey.publicKey);
        
        const account = {
            index: accountIndex,
            address,
            privateKey: accountKey.privateKey,
            publicKey: accountKey.publicKey,
            balance: '0',
            nonce: 0,
            createdAt: Date.now()
        };

        wallet.accounts.set(accountIndex, account);
        
        console.log(`✓ Account derived: ${walletId}/${accountIndex} (${address})`);
        this.emit('accountDerived', { walletId, accountIndex, address });
        
        return account;
    }

    /**
     * Sign message with identity
     */
    signMessage(identityId, message) {
        const identity = this.identities.get(identityId);
        if (!identity) {
            throw new Error(`Identity ${identityId} not found`);
        }

        const signature = crypto.sign('sha256', Buffer.from(message), identity.keyPair.privateKey);
        
        return {
            message,
            signature: signature.toString('hex'),
            signer: identity.did,
            timestamp: Date.now()
        };
    }

    /**
     * Verify message signature
     */
    verifyMessage(signedMessage) {
        const identity = this.resolveDID(signedMessage.signer);
        if (!identity) {
            return false;
        }

        try {
            const isValid = crypto.verify(
                'sha256',
                Buffer.from(signedMessage.message),
                identity.keyPair.publicKey,
                Buffer.from(signedMessage.signature, 'hex')
            );
            
            return isValid;
        } catch (error) {
            return false;
        }
    }

    /**
     * Register verifier
     */
    registerVerifier(name, verifier) {
        if (typeof verifier.verify !== 'function') {
            throw new Error('Verifier must have verify method');
        }

        this.verifiers.set(name, verifier);
        console.log(`✓ Verifier registered: ${name}`);
        this.emit('verifierRegistered', { name });
        
        return true;
    }

    /**
     * Register built-in verifiers
     */
    registerBuiltInVerifiers() {
        // Age verifier
        this.registerVerifier('age', {
            verify: (claims) => {
                const birthDate = new Date(claims.birthDate);
                const age = Math.floor((Date.now() - birthDate.getTime()) / (365.25 * 24 * 60 * 60 * 1000));
                return age >= 18;
            }
        });

        // Email verifier
        this.registerVerifier('email', {
            verify: (claims) => {
                const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                return emailRegex.test(claims.email);
            }
        });
    }

    /**
     * Generate key pair
     */
    generateKeyPair() {
        const { publicKey, privateKey } = crypto.generateKeyPairSync('rsa', {
            modulusLength: 2048,
            publicKeyEncoding: { type: 'spki', format: 'pem' },
            privateKeyEncoding: { type: 'pkcs8', format: 'pem' }
        });

        return { publicKey, privateKey };
    }

    /**
     * Generate DID
     */
    generateDID(method, publicKey) {
        const identifier = crypto.createHash('sha256')
            .update(publicKey)
            .digest('hex')
            .substring(0, 32);
        
        return `${method}:${identifier}`;
    }

    /**
     * Generate mnemonic
     */
    generateMnemonic() {
        // Simplified mnemonic generation
        const words = [
            'abandon', 'ability', 'able', 'about', 'above', 'absent', 'absorb', 'abstract',
            'absurd', 'abuse', 'access', 'accident', 'account', 'accuse', 'achieve', 'acid'
        ];
        
        const mnemonic = [];
        for (let i = 0; i < 12; i++) {
            mnemonic.push(words[Math.floor(Math.random() * words.length)]);
        }
        
        return mnemonic.join(' ');
    }

    /**
     * Derive master key from mnemonic
     */
    deriveMasterKey(mnemonic) {
        return crypto.createHash('sha512').update(mnemonic).digest('hex');
    }

    /**
     * Derive account key
     */
    deriveAccountKey(masterKey, accountIndex) {
        const seed = `${masterKey}_${accountIndex}`;
        const privateKey = crypto.createHash('sha256').update(seed).digest('hex');
        const publicKey = crypto.createHash('sha256').update(privateKey).digest('hex');
        
        return { privateKey, publicKey };
    }

    /**
     * Convert key to address
     */
    keyToAddress(publicKey) {
        return `0x${crypto.createHash('sha256').update(publicKey).digest('hex').substring(0, 40)}`;
    }

    /**
     * Sign credential
     */
    signCredential(claims, privateKey) {
        const data = JSON.stringify(claims);
        return crypto.sign('sha256', Buffer.from(data), privateKey).toString('hex');
    }

    /**
     * Verify signature
     */
    verifySignature(claims, signature, publicKey) {
        try {
            const data = JSON.stringify(claims);
            return crypto.verify('sha256', Buffer.from(data), publicKey, Buffer.from(signature, 'hex'));
        } catch (error) {
            return false;
        }
    }

    /**
     * Generate unique IDs
     */
    generateCredentialId() {
        return `vc_${Date.now()}_${crypto.randomBytes(8).toString('hex')}`;
    }

    /**
     * Get identity statistics
     */
    getIdentityStats(identityId) {
        const identity = this.identities.get(identityId);
        if (!identity) {
            throw new Error(`Identity ${identityId} not found`);
        }

        return {
            id: identity.id,
            did: identity.did,
            credentialsCount: identity.credentials.length,
            verifications: identity.verifications,
            createdAt: identity.createdAt
        };
    }

    /**
     * Get system statistics
     */
    getStats() {
        return {
            identities: this.identities.size,
            wallets: this.wallets.size,
            credentials: this.credentials.size,
            verifiers: this.verifiers.size,
            didRegistry: this.didRegistry.size,
            defaultMethod: this.defaultMethod
        };
    }
}

module.exports = { DecentralizedIdentity }; 