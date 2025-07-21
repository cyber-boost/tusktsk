/**
 * DeFi Protocols and Token Economics
 * Goal 12.3 Implementation
 */

const EventEmitter = require('events');
const crypto = require('crypto');

class DeFiProtocols extends EventEmitter {
    constructor(options = {}) {
        super();
        this.tokens = new Map();
        this.liquidityPools = new Map();
        this.stakingPools = new Map();
        this.yieldFarms = new Map();
        this.lendingPools = new Map();
        this.users = new Map();
        this.defaultSlippage = options.defaultSlippage || 0.005; // 0.5%
        this.protocolFee = options.protocolFee || 0.003; // 0.3%
        
        this.initializeDefaultTokens();
    }

    /**
     * Create ERC-20 token
     */
    createToken(tokenId, config) {
        const token = {
            id: tokenId,
            name: config.name,
            symbol: config.symbol,
            decimals: config.decimals || 18,
            totalSupply: config.totalSupply || '1000000',
            balances: new Map(),
            allowances: new Map(),
            createdAt: Date.now(),
            transfers: 0
        };

        // Mint initial supply to creator
        if (config.creator) {
            token.balances.set(config.creator, token.totalSupply);
        }

        this.tokens.set(tokenId, token);
        console.log(`✓ Token created: ${tokenId} (${config.symbol})`);
        this.emit('tokenCreated', { tokenId, symbol: config.symbol });
        
        return token;
    }

    /**
     * Transfer tokens
     */
    transfer(tokenId, from, to, amount) {
        const token = this.tokens.get(tokenId);
        if (!token) {
            throw new Error(`Token ${tokenId} not found`);
        }

        const fromBalance = parseFloat(token.balances.get(from) || '0');
        const amountFloat = parseFloat(amount);

        if (fromBalance < amountFloat) {
            throw new Error('Insufficient balance');
        }

        // Update balances
        token.balances.set(from, (fromBalance - amountFloat).toString());
        const toBalance = parseFloat(token.balances.get(to) || '0');
        token.balances.set(to, (toBalance + amountFloat).toString());
        
        token.transfers++;
        
        this.emit('tokenTransfer', { tokenId, from, to, amount });
        return true;
    }

    /**
     * Create liquidity pool
     */
    createLiquidityPool(poolId, tokenA, tokenB, initialRatio = 1) {
        if (!this.tokens.has(tokenA) || !this.tokens.has(tokenB)) {
            throw new Error('One or both tokens not found');
        }

        const pool = {
            id: poolId,
            tokenA,
            tokenB,
            reserveA: '0',
            reserveB: '0',
            totalLiquidity: '0',
            liquidityProviders: new Map(),
            swapCount: 0,
            volume24h: '0',
            fees24h: '0',
            createdAt: Date.now()
        };

        this.liquidityPools.set(poolId, pool);
        console.log(`✓ Liquidity pool created: ${poolId} (${tokenA}/${tokenB})`);
        this.emit('poolCreated', { poolId, tokenA, tokenB });
        
        return pool;
    }

    /**
     * Add liquidity to pool
     */
    addLiquidity(poolId, provider, amountA, amountB) {
        const pool = this.liquidityPools.get(poolId);
        if (!pool) {
            throw new Error(`Pool ${poolId} not found`);
        }

        const reserveA = parseFloat(pool.reserveA);
        const reserveB = parseFloat(pool.reserveB);
        const totalLiquidity = parseFloat(pool.totalLiquidity);

        // Calculate liquidity tokens to mint
        let liquidityTokens;
        if (totalLiquidity === 0) {
            liquidityTokens = Math.sqrt(parseFloat(amountA) * parseFloat(amountB));
        } else {
            const liquidityA = (parseFloat(amountA) * totalLiquidity) / reserveA;
            const liquidityB = (parseFloat(amountB) * totalLiquidity) / reserveB;
            liquidityTokens = Math.min(liquidityA, liquidityB);
        }

        // Update pool reserves
        pool.reserveA = (reserveA + parseFloat(amountA)).toString();
        pool.reserveB = (reserveB + parseFloat(amountB)).toString();
        pool.totalLiquidity = (totalLiquidity + liquidityTokens).toString();

        // Update provider's liquidity
        const currentLiquidity = parseFloat(pool.liquidityProviders.get(provider) || '0');
        pool.liquidityProviders.set(provider, (currentLiquidity + liquidityTokens).toString());

        this.emit('liquidityAdded', { poolId, provider, amountA, amountB, liquidityTokens });
        return liquidityTokens;
    }

    /**
     * Swap tokens
     */
    swap(poolId, tokenIn, amountIn, minAmountOut, user) {
        const pool = this.liquidityPools.get(poolId);
        if (!pool) {
            throw new Error(`Pool ${poolId} not found`);
        }

        let reserveIn, reserveOut, tokenOut;
        
        if (tokenIn === pool.tokenA) {
            reserveIn = parseFloat(pool.reserveA);
            reserveOut = parseFloat(pool.reserveB);
            tokenOut = pool.tokenB;
        } else if (tokenIn === pool.tokenB) {
            reserveIn = parseFloat(pool.reserveB);
            reserveOut = parseFloat(pool.reserveA);
            tokenOut = pool.tokenA;
        } else {
            throw new Error('Token not in pool');
        }

        // Calculate output amount using constant product formula
        const amountInFloat = parseFloat(amountIn);
        const amountInWithFee = amountInFloat * (1 - this.protocolFee);
        const amountOut = (amountInWithFee * reserveOut) / (reserveIn + amountInWithFee);

        if (amountOut < parseFloat(minAmountOut)) {
            throw new Error('Slippage too high');
        }

        // Update reserves
        if (tokenIn === pool.tokenA) {
            pool.reserveA = (reserveIn + amountInFloat).toString();
            pool.reserveB = (reserveOut - amountOut).toString();
        } else {
            pool.reserveB = (reserveIn + amountInFloat).toString();
            pool.reserveA = (reserveOut - amountOut).toString();
        }

        pool.swapCount++;
        
        // Update 24h volume
        const currentVolume = parseFloat(pool.volume24h);
        pool.volume24h = (currentVolume + amountInFloat).toString();

        this.emit('tokenSwapped', { poolId, tokenIn, tokenOut, amountIn, amountOut, user });
        return { tokenOut, amountOut: amountOut.toString() };
    }

    /**
     * Create staking pool
     */
    createStakingPool(poolId, stakingToken, rewardToken, rewardRate) {
        const pool = {
            id: poolId,
            stakingToken,
            rewardToken,
            rewardRate: parseFloat(rewardRate), // tokens per second
            totalStaked: '0',
            rewardPerTokenStored: '0',
            lastUpdateTime: Date.now(),
            stakers: new Map(),
            createdAt: Date.now()
        };

        this.stakingPools.set(poolId, pool);
        console.log(`✓ Staking pool created: ${poolId}`);
        this.emit('stakingPoolCreated', { poolId, stakingToken, rewardToken });
        
        return pool;
    }

    /**
     * Stake tokens
     */
    stake(poolId, user, amount) {
        const pool = this.stakingPools.get(poolId);
        if (!pool) {
            throw new Error(`Staking pool ${poolId} not found`);
        }

        this.updateRewards(poolId);

        const currentStake = parseFloat(pool.stakers.get(user)?.staked || '0');
        const newStake = currentStake + parseFloat(amount);
        
        pool.stakers.set(user, {
            staked: newStake.toString(),
            rewardPerTokenPaid: pool.rewardPerTokenStored,
            rewards: pool.stakers.get(user)?.rewards || '0'
        });

        pool.totalStaked = (parseFloat(pool.totalStaked) + parseFloat(amount)).toString();

        this.emit('tokensStaked', { poolId, user, amount });
        return true;
    }

    /**
     * Create yield farm
     */
    createYieldFarm(farmId, lpToken, rewardTokens, rewardRates) {
        const farm = {
            id: farmId,
            lpToken,
            rewardTokens,
            rewardRates,
            totalStaked: '0',
            farmers: new Map(),
            multiplier: 1,
            createdAt: Date.now()
        };

        this.yieldFarms.set(farmId, farm);
        console.log(`✓ Yield farm created: ${farmId}`);
        this.emit('yieldFarmCreated', { farmId, lpToken });
        
        return farm;
    }

    /**
     * Update rewards for staking pool
     */
    updateRewards(poolId) {
        const pool = this.stakingPools.get(poolId);
        if (!pool) return;

        const now = Date.now();
        const timeElapsed = (now - pool.lastUpdateTime) / 1000; // seconds
        
        if (parseFloat(pool.totalStaked) > 0) {
            const rewardPerToken = (pool.rewardRate * timeElapsed) / parseFloat(pool.totalStaked);
            pool.rewardPerTokenStored = (parseFloat(pool.rewardPerTokenStored) + rewardPerToken).toString();
        }
        
        pool.lastUpdateTime = now;
    }

    /**
     * Create lending pool
     */
    createLendingPool(poolId, asset, interestRate) {
        const pool = {
            id: poolId,
            asset,
            interestRate: parseFloat(interestRate), // APY
            totalDeposits: '0',
            totalBorrows: '0',
            utilizationRate: '0',
            depositors: new Map(),
            borrowers: new Map(),
            createdAt: Date.now()
        };

        this.lendingPools.set(poolId, pool);
        console.log(`✓ Lending pool created: ${poolId}`);
        this.emit('lendingPoolCreated', { poolId, asset });
        
        return pool;
    }

    /**
     * Deposit to lending pool
     */
    deposit(poolId, user, amount) {
        const pool = this.lendingPools.get(poolId);
        if (!pool) {
            throw new Error(`Lending pool ${poolId} not found`);
        }

        const currentDeposit = parseFloat(pool.depositors.get(user) || '0');
        pool.depositors.set(user, (currentDeposit + parseFloat(amount)).toString());
        pool.totalDeposits = (parseFloat(pool.totalDeposits) + parseFloat(amount)).toString();

        this.updateUtilizationRate(poolId);
        
        this.emit('deposited', { poolId, user, amount });
        return true;
    }

    /**
     * Update utilization rate
     */
    updateUtilizationRate(poolId) {
        const pool = this.lendingPools.get(poolId);
        if (!pool) return;

        const totalDeposits = parseFloat(pool.totalDeposits);
        const totalBorrows = parseFloat(pool.totalBorrows);
        
        if (totalDeposits > 0) {
            pool.utilizationRate = (totalBorrows / totalDeposits).toString();
        } else {
            pool.utilizationRate = '0';
        }
    }

    /**
     * Initialize default tokens
     */
    initializeDefaultTokens() {
        // Create default tokens
        this.createToken('USDC', {
            name: 'USD Coin',
            symbol: 'USDC',
            decimals: 6,
            totalSupply: '1000000000',
            creator: 'system'
        });

        this.createToken('WETH', {
            name: 'Wrapped Ether',
            symbol: 'WETH',
            decimals: 18,
            totalSupply: '1000000',
            creator: 'system'
        });
    }

    /**
     * Get token balance
     */
    getBalance(tokenId, address) {
        const token = this.tokens.get(tokenId);
        if (!token) {
            throw new Error(`Token ${tokenId} not found`);
        }
        
        return token.balances.get(address) || '0';
    }

    /**
     * Get pool statistics
     */
    getPoolStats(poolId) {
        const pool = this.liquidityPools.get(poolId);
        if (!pool) {
            throw new Error(`Pool ${poolId} not found`);
        }

        return {
            id: pool.id,
            tokenA: pool.tokenA,
            tokenB: pool.tokenB,
            reserveA: pool.reserveA,
            reserveB: pool.reserveB,
            totalLiquidity: pool.totalLiquidity,
            swapCount: pool.swapCount,
            volume24h: pool.volume24h
        };
    }

    /**
     * Get system statistics
     */
    getStats() {
        return {
            tokens: this.tokens.size,
            liquidityPools: this.liquidityPools.size,
            stakingPools: this.stakingPools.size,
            yieldFarms: this.yieldFarms.size,
            lendingPools: this.lendingPools.size,
            users: this.users.size,
            defaultSlippage: this.defaultSlippage,
            protocolFee: this.protocolFee
        };
    }
}

module.exports = { DeFiProtocols }; 