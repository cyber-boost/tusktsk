# Blockchain Integration in C# with TuskLang

## Overview

Blockchain Integration involves incorporating blockchain technology into applications for decentralized, secure, and transparent operations. This guide covers how to implement blockchain integration using C# and TuskLang configuration for building trustless, decentralized applications.

## Table of Contents

- [Blockchain Integration Concepts](#blockchain-integration-concepts)
- [TuskLang Blockchain Configuration](#tusklang-blockchain-configuration)
- [Smart Contracts](#smart-contracts)
- [C# Blockchain Example](#c-blockchain-example)
- [Wallet Integration](#wallet-integration)
- [DeFi Integration](#defi-integration)
- [Best Practices](#best-practices)

## Blockchain Integration Concepts

- **Smart Contracts**: Self-executing contracts with code
- **Wallets**: Digital wallets for managing cryptocurrencies
- **Transactions**: Operations on the blockchain
- **Blocks**: Groups of transactions
- **Consensus**: Agreement mechanism for validating transactions
- **DeFi**: Decentralized Finance applications

## TuskLang Blockchain Configuration

```ini
# blockchain.tsk
[blockchain]
enabled = @env("BLOCKCHAIN_ENABLED", "true")
network = @env("BLOCKCHAIN_NETWORK", "ethereum")
environment = @env("BLOCKCHAIN_ENVIRONMENT", "testnet")

[ethereum]
mainnet_rpc_url = @env.secure("ETHEREUM_MAINNET_RPC")
testnet_rpc_url = @env.secure("ETHEREUM_TESTNET_RPC")
infura_project_id = @env.secure("INFURA_PROJECT_ID")
alchemy_api_key = @env.secure("ALCHEMY_API_KEY")

[smart_contracts]
token_contract = @env("TOKEN_CONTRACT_ADDRESS", "0x1234567890123456789012345678901234567890")
nft_contract = @env("NFT_CONTRACT_ADDRESS", "0x0987654321098765432109876543210987654321")
defi_contract = @env("DEFI_CONTRACT_ADDRESS", "0x1111111111111111111111111111111111111111")

[wallet]
private_key = @env.secure("WALLET_PRIVATE_KEY")
public_address = @env("WALLET_PUBLIC_ADDRESS", "0x2222222222222222222222222222222222222222")
gas_limit = @env("WALLET_GAS_LIMIT", "21000")
gas_price = @env("WALLET_GAS_PRICE", "20000000000")

[defi]
uniswap_router = @env("UNISWAP_ROUTER_ADDRESS", "0x7a250d5630B4cF539739dF2C5dAcb4c659F2488D")
sushiswap_router = @env("SUSHISWAP_ROUTER_ADDRESS", "0xd9e1cE17f2641f24aE83637ab66a2cca9C378B9F")
aave_lending_pool = @env("AAVE_LENDING_POOL_ADDRESS", "0x7d2768dE32b0b80b7a3454c06BdAc94A69DDc7A9")

[nft]
opensea_api_key = @env.secure("OPENSEA_API_KEY")
ipfs_gateway = @env("IPFS_GATEWAY", "https://ipfs.io/ipfs/")
metadata_contract = @env("NFT_METADATA_CONTRACT", "0x1234567890123456789012345678901234567890")

[monitoring]
transaction_monitoring = @env("TRANSACTION_MONITORING", "true")
gas_price_monitoring = @env("GAS_PRICE_MONITORING", "true")
block_monitoring = @env("BLOCK_MONITORING", "true")
webhook_url = @env.secure("BLOCKCHAIN_WEBHOOK_URL")

[security]
private_key_encryption = @env("PRIVATE_KEY_ENCRYPTION", "true")
transaction_signing = @env("TRANSACTION_SIGNING", "true")
multi_sig_enabled = @env("MULTI_SIG_ENABLED", "false")
```

## Smart Contracts

```csharp
using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TuskLang;

public interface IBlockchainService
{
    Task<string> DeployContractAsync(string contractName, object[] constructorParams);
    Task<Contract> GetContractAsync(string contractAddress, string abi);
    Task<HexBigInteger> GetBalanceAsync(string address);
    Task<string> SendTransactionAsync(string to, decimal amount);
}

public interface ITokenService
{
    Task<string> DeployTokenAsync(string name, string symbol, int decimals, BigInteger totalSupply);
    Task<BigInteger> GetTokenBalanceAsync(string contractAddress, string walletAddress);
    Task<string> TransferTokenAsync(string contractAddress, string to, BigInteger amount);
    Task<string> ApproveTokenAsync(string contractAddress, string spender, BigInteger amount);
}

public interface INFTService
{
    Task<string> DeployNFTAsync(string name, string symbol);
    Task<string> MintNFTAsync(string contractAddress, string to, string tokenURI);
    Task<string> GetNFTOwnerAsync(string contractAddress, BigInteger tokenId);
    Task<string> TransferNFTAsync(string contractAddress, string from, string to, BigInteger tokenId);
}

public interface IDeFiService
{
    Task<string> SwapTokensAsync(string routerAddress, string tokenIn, string tokenOut, BigInteger amountIn, BigInteger amountOutMin);
    Task<string> AddLiquidityAsync(string routerAddress, string tokenA, string tokenB, BigInteger amountADesired, BigInteger amountBDesired);
    Task<string> RemoveLiquidityAsync(string routerAddress, string tokenA, string tokenB, BigInteger liquidity);
    Task<BigInteger> GetReservesAsync(string pairAddress);
}

public class EthereumService : IBlockchainService
{
    private readonly Web3 _web3;
    private readonly IConfiguration _config;
    private readonly ILogger<EthereumService> _logger;
    private readonly string _privateKey;
    private readonly string _publicAddress;

    public EthereumService(IConfiguration config, ILogger<EthereumService> logger)
    {
        _config = config;
        _logger = logger;
        
        var network = _config["blockchain:network"];
        var rpcUrl = network == "mainnet" 
            ? _config["ethereum:mainnet_rpc_url"] 
            : _config["ethereum:testnet_rpc_url"];
        
        _web3 = new Web3(rpcUrl);
        _privateKey = _config["wallet:private_key"];
        _publicAddress = _config["wallet:public_address"];
        
        _web3.TransactionManager.DefaultGas = HexBigInteger.Parse(_config["wallet:gas_limit"]);
        _web3.TransactionManager.DefaultGasPrice = HexBigInteger.Parse(_config["wallet:gas_price"]);
    }

    public async Task<string> DeployContractAsync(string contractName, object[] constructorParams)
    {
        try
        {
            var contractBytecode = GetContractBytecode(contractName);
            var contractAbi = GetContractAbi(contractName);

            var deploymentMessage = new DeploymentMessage
            {
                FromAddress = _publicAddress,
                Gas = HexBigInteger.Parse(_config["wallet:gas_limit"]),
                GasPrice = HexBigInteger.Parse(_config["wallet:gas_price"])
            };

            // Add constructor parameters
            for (int i = 0; i < constructorParams.Length; i++)
            {
                deploymentMessage.GetType().GetProperty($"Param{i}")?.SetValue(deploymentMessage, constructorParams[i]);
            }

            var deploymentHandler = _web3.Eth.GetContractDeploymentHandler<DeploymentMessage>();
            var deploymentReceipt = await deploymentHandler.SendRequestAndWaitForReceiptAsync(deploymentMessage);

            _logger.LogInformation("Deployed contract {ContractName} at address {Address}", 
                contractName, deploymentReceipt.ContractAddress);

            return deploymentReceipt.ContractAddress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying contract {ContractName}", contractName);
            throw;
        }
    }

    public async Task<Contract> GetContractAsync(string contractAddress, string abi)
    {
        try
        {
            return _web3.Eth.GetContract(abi, contractAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract at address {Address}", contractAddress);
            throw;
        }
    }

    public async Task<HexBigInteger> GetBalanceAsync(string address)
    {
        try
        {
            return await _web3.Eth.GetBalance.SendRequestAsync(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for address {Address}", address);
            throw;
        }
    }

    public async Task<string> SendTransactionAsync(string to, decimal amount)
    {
        try
        {
            var transaction = new TransactionInput
            {
                From = _publicAddress,
                To = to,
                Value = new HexBigInteger(Web3.Convert.ToWei(amount)),
                Gas = HexBigInteger.Parse(_config["wallet:gas_limit"]),
                GasPrice = HexBigInteger.Parse(_config["wallet:gas_price"])
            };

            var transactionHash = await _web3.Eth.TransactionManager.SendTransactionAsync(transaction);
            
            _logger.LogInformation("Sent transaction {Hash} to {To} for {Amount} ETH", 
                transactionHash, to, amount);

            return transactionHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transaction to {To}", to);
            throw;
        }
    }

    private string GetContractBytecode(string contractName)
    {
        // Implementation to get contract bytecode from compiled contracts
        return contractName switch
        {
            "ERC20Token" => "0x608060405234801561001057600080fd5b506040516107e83803806107e88339818101604052602081101561003357600080fd5b810190808051906020019092919050505080600081905550506107888061005c6000396000f3fe608060405234801561001057600080fd5b506004361061004c5760003560e01c806318160ddd1461005157806370a082311461006f578063a9059cbb146100a7578063dd62ed3e146100d5575b600080fd5b61005961010d565b6040518082815260200191505060405180910390f35b6100916004803603602081101561008557600080fd5b8101908080359060200190929190505050610116565b6040518082815260200191505060405180910390f35b6100d3600480360360408110156100bd57600080fd5b81019080803590602001909291908035906020019092919050505061012e565b005b6100f7600480360360408110156100eb57600080fd5b8101908080359060200190929190803590602001909291905050506101a4565b6040518082815260200191505060405180910390f35b60008054905090565b60006020528060005260406000206000915090505481565b8060008082825401925050819055507f93c1f6bcfd688d57ea7b1699d3d06a65da48b705475cd3867352f05f431af50660016040518082815260200191505060405180910390a150565b600060408201905091905056fea265627a7a72315820f8b3dc5c6e8c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c64736f6c63430005100032",
            "ERC721NFT" => "0x608060405234801561001057600080fd5b506040516107e83803806107e88339818101604052602081101561003357600080fd5b810190808051906020019092919050505080600081905550506107888061005c6000396000f3fe608060405234801561001057600080fd5b506004361061004c5760003560e01c806318160ddd1461005157806370a082311461006f578063a9059cbb146100a7578063dd62ed3e146100d5575b600080fd5b61005961010d565b6040518082815260200191505060405180910390f35b6100916004803603602081101561008557600080fd5b8101908080359060200190929190505050610116565b6040518082815260200191505060405180910390f35b6100d3600480360360408110156100bd57600080fd5b81019080803590602001909291908035906020019092919050505061012e565b005b6100f7600480360360408110156100eb57600080fd5b8101908080359060200190929190803590602001909291905050506101a4565b6040518082815260200191505060405180910390f35b60008054905090565b60006020528060005260406000206000915090505481565b8060008082825401925050819055507f93c1f6bcfd688d57ea7b1699d3d06a65da48b705475cd3867352f05f431af50660016040518082815260200191505060405180910390a150565b600060408201905091905056fea265627a7a72315820f8b3dc5c6e8c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c64736f6c63430005100032",
            _ => throw new ArgumentException($"Unknown contract: {contractName}")
        };
    }

    private string GetContractAbi(string contractName)
    {
        // Implementation to get contract ABI from compiled contracts
        return contractName switch
        {
            "ERC20Token" => @"[{""constant"":true,""inputs"":[],""name"":""name"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""decimals"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""totalSupply"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":""balance"",""type"":""uint256""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]",
            "ERC721NFT" => @"[{""constant"":true,""inputs"":[],""name"":""name"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":true,""inputs"":[{""name"":""tokenId"",""type"":""uint256""}],""name"":""ownerOf"",""outputs"":[{""name"":"""",""type"":""address""}],""payable"":false,""stateMutability"":""view"",""type"":""function""},{""constant"":false,""inputs"":[{""name"":""to"",""type"":""address""},{""name"":""tokenId"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]",
            _ => throw new ArgumentException($"Unknown contract: {contractName}")
        };
    }
}

public class DeploymentMessage : ContractDeploymentMessage
{
    public string Param0 { get; set; }
    public string Param1 { get; set; }
    public int Param2 { get; set; }
    public BigInteger Param3 { get; set; }
}
```

## C# Blockchain Example

```csharp
using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TuskLang;

[ApiController]
[Route("api/[controller]")]
public class BlockchainController : ControllerBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly ITokenService _tokenService;
    private readonly INFTService _nftService;
    private readonly IDeFiService _defiService;
    private readonly IConfiguration _config;
    private readonly ILogger<BlockchainController> _logger;

    public BlockchainController(
        IBlockchainService blockchainService,
        ITokenService tokenService,
        INFTService nftService,
        IDeFiService defiService,
        IConfiguration config,
        ILogger<BlockchainController> logger)
    {
        _blockchainService = blockchainService;
        _tokenService = tokenService;
        _nftService = nftService;
        _defiService = defiService;
        _config = config;
        _logger = logger;
    }

    [HttpPost("deploy-token")]
    public async Task<IActionResult> DeployToken([FromBody] DeployTokenRequest request)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var contractAddress = await _tokenService.DeployTokenAsync(
                request.Name, 
                request.Symbol, 
                request.Decimals, 
                request.TotalSupply);

            _logger.LogInformation("Deployed token contract at address {Address}", contractAddress);

            return Ok(new { ContractAddress = contractAddress });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying token contract");
            return StatusCode(500, "Error deploying token contract");
        }
    }

    [HttpGet("token-balance/{contractAddress}/{walletAddress}")]
    public async Task<IActionResult> GetTokenBalance(string contractAddress, string walletAddress)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var balance = await _tokenService.GetTokenBalanceAsync(contractAddress, walletAddress);

            return Ok(new { Balance = balance.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token balance for contract {ContractAddress}", contractAddress);
            return StatusCode(500, "Error getting token balance");
        }
    }

    [HttpPost("transfer-token")]
    public async Task<IActionResult> TransferToken([FromBody] TransferTokenRequest request)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var transactionHash = await _tokenService.TransferTokenAsync(
                request.ContractAddress, 
                request.To, 
                request.Amount);

            _logger.LogInformation("Token transfer transaction: {Hash}", transactionHash);

            return Ok(new { TransactionHash = transactionHash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring tokens");
            return StatusCode(500, "Error transferring tokens");
        }
    }

    [HttpPost("deploy-nft")]
    public async Task<IActionResult> DeployNFT([FromBody] DeployNFTRequest request)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var contractAddress = await _nftService.DeployNFTAsync(request.Name, request.Symbol);

            _logger.LogInformation("Deployed NFT contract at address {Address}", contractAddress);

            return Ok(new { ContractAddress = contractAddress });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying NFT contract");
            return StatusCode(500, "Error deploying NFT contract");
        }
    }

    [HttpPost("mint-nft")]
    public async Task<IActionResult> MintNFT([FromBody] MintNFTRequest request)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var transactionHash = await _nftService.MintNFTAsync(
                request.ContractAddress, 
                request.To, 
                request.TokenURI);

            _logger.LogInformation("NFT mint transaction: {Hash}", transactionHash);

            return Ok(new { TransactionHash = transactionHash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error minting NFT");
            return StatusCode(500, "Error minting NFT");
        }
    }

    [HttpPost("swap-tokens")]
    public async Task<IActionResult> SwapTokens([FromBody] SwapTokensRequest request)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var routerAddress = _config["defi:uniswap_router"];
            var transactionHash = await _defiService.SwapTokensAsync(
                routerAddress,
                request.TokenIn,
                request.TokenOut,
                request.AmountIn,
                request.AmountOutMin);

            _logger.LogInformation("Token swap transaction: {Hash}", transactionHash);

            return Ok(new { TransactionHash = transactionHash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error swapping tokens");
            return StatusCode(500, "Error swapping tokens");
        }
    }

    [HttpGet("balance/{address}")]
    public async Task<IActionResult> GetBalance(string address)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var balance = await _blockchainService.GetBalanceAsync(address);

            return Ok(new { Balance = balance.Value.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for address {Address}", address);
            return StatusCode(500, "Error getting balance");
        }
    }

    [HttpPost("send-transaction")]
    public async Task<IActionResult> SendTransaction([FromBody] SendTransactionRequest request)
    {
        try
        {
            if (!bool.Parse(_config["blockchain:enabled"]))
                return BadRequest("Blockchain services are disabled");

            var transactionHash = await _blockchainService.SendTransactionAsync(request.To, request.Amount);

            return Ok(new { TransactionHash = transactionHash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transaction");
            return StatusCode(500, "Error sending transaction");
        }
    }
}

// Request/Response Models
public class DeployTokenRequest
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public BigInteger TotalSupply { get; set; }
}

public class TransferTokenRequest
{
    public string ContractAddress { get; set; }
    public string To { get; set; }
    public BigInteger Amount { get; set; }
}

public class DeployNFTRequest
{
    public string Name { get; set; }
    public string Symbol { get; set; }
}

public class MintNFTRequest
{
    public string ContractAddress { get; set; }
    public string To { get; set; }
    public string TokenURI { get; set; }
}

public class SwapTokensRequest
{
    public string TokenIn { get; set; }
    public string TokenOut { get; set; }
    public BigInteger AmountIn { get; set; }
    public BigInteger AmountOutMin { get; set; }
}

public class SendTransactionRequest
{
    public string To { get; set; }
    public decimal Amount { get; set; }
}
```

## Wallet Integration

```csharp
public interface IWalletService
{
    Task<WalletInfo> CreateWalletAsync();
    Task<string> SignTransactionAsync(string transactionData);
    Task<bool> ValidateAddressAsync(string address);
    Task<TransactionInfo> GetTransactionAsync(string transactionHash);
}

public class WalletService : IWalletService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WalletService> _logger;
    private readonly Web3 _web3;

    public WalletService(IConfiguration config, ILogger<WalletService> logger)
    {
        _config = config;
        _logger = logger;
        
        var network = _config["blockchain:network"];
        var rpcUrl = network == "mainnet" 
            ? _config["ethereum:mainnet_rpc_url"] 
            : _config["ethereum:testnet_rpc_url"];
        
        _web3 = new Web3(rpcUrl);
    }

    public async Task<WalletInfo> CreateWalletAsync()
    {
        try
        {
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var address = ecKey.GetPublicAddress();

            var walletInfo = new WalletInfo
            {
                Address = address,
                PrivateKey = privateKey,
                CreatedOn = DateTime.UtcNow
            };

            _logger.LogInformation("Created new wallet: {Address}", address);
            return walletInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet");
            throw;
        }
    }

    public async Task<string> SignTransactionAsync(string transactionData)
    {
        try
        {
            var privateKey = _config["wallet:private_key"];
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            
            // Implementation for signing transaction data
            // This would depend on the specific transaction format
            
            _logger.LogInformation("Signed transaction with account {Address}", account.Address);
            return "signed_transaction_hash";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing transaction");
            throw;
        }
    }

    public async Task<bool> ValidateAddressAsync(string address)
    {
        try
        {
            return Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating address {Address}", address);
            return false;
        }
    }

    public async Task<TransactionInfo> GetTransactionAsync(string transactionHash)
    {
        try
        {
            var transaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash);
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            return new TransactionInfo
            {
                Hash = transactionHash,
                From = transaction.From,
                To = transaction.To,
                Value = transaction.Value.Value.ToString(),
                GasUsed = receipt.GasUsed.Value.ToString(),
                Status = receipt.Status.Value == 1 ? "Success" : "Failed",
                BlockNumber = receipt.BlockNumber.Value.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction {Hash}", transactionHash);
            throw;
        }
    }
}

public class WalletInfo
{
    public string Address { get; set; }
    public string PrivateKey { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class TransactionInfo
{
    public string Hash { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Value { get; set; }
    public string GasUsed { get; set; }
    public string Status { get; set; }
    public string BlockNumber { get; set; }
}
```

## DeFi Integration

```csharp
public class DeFiService : IDeFiService
{
    private readonly Web3 _web3;
    private readonly IConfiguration _config;
    private readonly ILogger<DeFiService> _logger;

    public DeFiService(IConfiguration config, ILogger<DeFiService> logger)
    {
        _config = config;
        _logger = logger;
        
        var network = _config["blockchain:network"];
        var rpcUrl = network == "mainnet" 
            ? _config["ethereum:mainnet_rpc_url"] 
            : _config["ethereum:testnet_rpc_url"];
        
        _web3 = new Web3(rpcUrl);
    }

    public async Task<string> SwapTokensAsync(string routerAddress, string tokenIn, string tokenOut, BigInteger amountIn, BigInteger amountOutMin)
    {
        try
        {
            var routerContract = _web3.Eth.GetContract(GetUniswapRouterAbi(), routerAddress);
            var swapFunction = routerContract.GetFunction("swapExactTokensForTokens");

            var path = new string[] { tokenIn, tokenOut };
            var deadline = DateTimeOffset.UtcNow.AddMinutes(20).ToUnixTimeSeconds();

            var transactionHash = await swapFunction.SendTransactionAsync(
                from: _config["wallet:public_address"],
                gas: HexBigInteger.Parse(_config["wallet:gas_limit"]),
                gasPrice: HexBigInteger.Parse(_config["wallet:gas_price"]),
                value: new HexBigInteger(0),
                amountIn,
                amountOutMin,
                path,
                _config["wallet:public_address"],
                deadline);

            _logger.LogInformation("Token swap transaction: {Hash}", transactionHash);
            return transactionHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error swapping tokens");
            throw;
        }
    }

    public async Task<string> AddLiquidityAsync(string routerAddress, string tokenA, string tokenB, BigInteger amountADesired, BigInteger amountBDesired)
    {
        try
        {
            var routerContract = _web3.Eth.GetContract(GetUniswapRouterAbi(), routerAddress);
            var addLiquidityFunction = routerContract.GetFunction("addLiquidity");

            var deadline = DateTimeOffset.UtcNow.AddMinutes(20).ToUnixTimeSeconds();

            var transactionHash = await addLiquidityFunction.SendTransactionAsync(
                from: _config["wallet:public_address"],
                gas: HexBigInteger.Parse(_config["wallet:gas_limit"]),
                gasPrice: HexBigInteger.Parse(_config["wallet:gas_price"]),
                value: new HexBigInteger(0),
                tokenA,
                tokenB,
                amountADesired,
                amountBDesired,
                0, // amountAMin
                0, // amountBMin
                _config["wallet:public_address"],
                deadline);

            _logger.LogInformation("Add liquidity transaction: {Hash}", transactionHash);
            return transactionHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding liquidity");
            throw;
        }
    }

    public async Task<string> RemoveLiquidityAsync(string routerAddress, string tokenA, string tokenB, BigInteger liquidity)
    {
        try
        {
            var routerContract = _web3.Eth.GetContract(GetUniswapRouterAbi(), routerAddress);
            var removeLiquidityFunction = routerContract.GetFunction("removeLiquidity");

            var deadline = DateTimeOffset.UtcNow.AddMinutes(20).ToUnixTimeSeconds();

            var transactionHash = await removeLiquidityFunction.SendTransactionAsync(
                from: _config["wallet:public_address"],
                gas: HexBigInteger.Parse(_config["wallet:gas_limit"]),
                gasPrice: HexBigInteger.Parse(_config["wallet:gas_price"]),
                value: new HexBigInteger(0),
                tokenA,
                tokenB,
                liquidity,
                0, // amountAMin
                0, // amountBMin
                _config["wallet:public_address"],
                deadline);

            _logger.LogInformation("Remove liquidity transaction: {Hash}", transactionHash);
            return transactionHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing liquidity");
            throw;
        }
    }

    public async Task<BigInteger> GetReservesAsync(string pairAddress)
    {
        try
        {
            var pairContract = _web3.Eth.GetContract(GetUniswapPairAbi(), pairAddress);
            var getReservesFunction = pairContract.GetFunction("getReserves");

            var reserves = await getReservesFunction.CallAsync<object>();
            // Parse reserves from the result
            return BigInteger.Zero; // Simplified for this example
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reserves for pair {Address}", pairAddress);
            throw;
        }
    }

    private string GetUniswapRouterAbi()
    {
        // Simplified Uniswap Router ABI
        return @"[{""inputs"":[{""internalType"":""uint256"",""name"":""amountIn"",""type"":""uint256""},{""internalType"":""uint256"",""name"":""amountOutMin"",""type"":""uint256""},{""internalType"":""address[]"",""name"":""path"",""type"":""address[]""},{""internalType"":""address"",""name"":""to"",""type"":""address""},{""internalType"":""uint256"",""name"":""deadline"",""type"":""uint256""}],""name"":""swapExactTokensForTokens"",""outputs"":[{""internalType"":""uint256[]"",""name"":""amounts"",""type"":""uint256[]""}],""stateMutability"":""nonpayable"",""type"":""function""}]";
    }

    private string GetUniswapPairAbi()
    {
        // Simplified Uniswap Pair ABI
        return @"[{""inputs"":[],""name"":""getReserves"",""outputs"":[{""internalType"":""uint112"",""name"":""_reserve0"",""type"":""uint112""},{""internalType"":""uint112"",""name"":""_reserve1"",""type"":""uint112""},{""internalType"":""uint32"",""name"":""_blockTimestampLast"",""type"":""uint32""}],""stateMutability"":""view"",""type"":""function""}]";
    }
}
```

## Best Practices

1. **Secure private key management**
2. **Implement proper error handling**
3. **Use gas estimation for transactions**
4. **Monitor transaction status**
5. **Implement retry mechanisms**
6. **Use appropriate network (testnet vs mainnet)**
7. **Validate all inputs and addresses**

## Conclusion

Blockchain Integration with C# and TuskLang enables building decentralized, secure, and transparent applications. By leveraging TuskLang for configuration and blockchain patterns, you can create systems that are trustless, immutable, and aligned with modern blockchain practices. 