/**
 * DeFi Protocols and Token Economics
 * Goal 12.3 Implementation
 */

const EventEmitter = require("events");

class DeFiProtocols extends EventEmitter {
    constructor(options = {}) {
        super();
        this.tokens = new Map();
        this.liquidityPools = new Map();
        this.stakingPools = new Map();
        this.defaultSlippage = options.defaultSlippage || 0.005;
        
        this.initializeDefaultTokens();
    }

    createToken(tokenId, config) {
        const token = {
            id: tokenId,
            name: config.name,
            symbol: config.symbol,
            totalSupply: config.totalSupply || "1000000",
            balances: new Map(),
            createdAt: Date.now()
        };
        
        if (config.creator) {
            token.balances.set(config.creator, token.totalSupply);
        }
        
        this.tokens.set(tokenId, token);
        console.log(`✓ Token created: ${tokenId}`);
        return token;
    }

    createLiquidityPool(poolId, tokenA, tokenB) {
        const pool = {
            id: poolId,
            tokenA,
            tokenB,
            reserveA: "0",
            reserveB: "0",
            totalLiquidity: "0",
            createdAt: Date.now()
        };
        
        this.liquidityPools.set(poolId, pool);
        console.log(`✓ Liquidity pool created: ${poolId}`);
        return pool;
    }

    addLiquidity(poolId, provider, amountA, amountB) {
        const pool = this.liquidityPools.get(poolId);
        if (!pool) throw new Error(`Pool ${poolId} not found`);
        
        const liquidityTokens = Math.sqrt(parseFloat(amountA) * parseFloat(amountB));
        console.log(`✓ Liquidity added: ${liquidityTokens}`);
        return liquidityTokens;
    }

    swap(poolId, tokenIn, amountIn, minAmountOut, user) {
        const pool = this.liquidityPools.get(poolId);
        if (!pool) throw new Error(`Pool ${poolId} not found`);
        
        const amountOut = parseFloat(amountIn) * 0.99; // Simple 1% fee
        const tokenOut = tokenIn === pool.tokenA ? pool.tokenB : pool.tokenA;
        
        console.log(`✓ Swap executed: ${amountIn} ${tokenIn} -> ${amountOut} ${tokenOut}`);
        return { tokenOut, amountOut: amountOut.toString() };
    }

    createStakingPool(poolId, stakingToken, rewardToken, rewardRate) {
        const pool = {
            id: poolId,
            stakingToken,
            rewardToken,
            rewardRate: parseFloat(rewardRate),
            totalStaked: "0",
            createdAt: Date.now()
        };
        
        this.stakingPools.set(poolId, pool);
        console.log(`✓ Staking pool created: ${poolId}`);
        return pool;
    }

    initializeDefaultTokens() {
        this.createToken("USDC", {
            name: "USD Coin",
            symbol: "USDC",
            totalSupply: "1000000000",
            creator: "system"
        });
        
        this.createToken("WETH", {
            name: "Wrapped Ether",
            symbol: "WETH",
            totalSupply: "1000000",
            creator: "system"
        });
    }

    getStats() {
        return {
            tokens: this.tokens.size,
            liquidityPools: this.liquidityPools.size,
            stakingPools: this.stakingPools.size
        };
    }
}

module.exports = { DeFiProtocols };
