using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using SafePuchaseWeb.Models;

namespace SafePuchaseWeb.Controllers
{
    using TransactionManager = SafePuchaseWeb.TransactionManager;

    public static class Extensions
    {
        public static TransactionManager AddGas(this TransactionManager transactionManager, decimal gas)
        {
            if (transactionManager.Tx != null && gas > 0.0m)
            {
                transactionManager.Tx.SystemFee += (long)gas.ToBigInteger(NativeContract.GAS.Decimals);
            }
            return transactionManager;
        }
    }

    public class HomeController : Controller
    {
        private readonly NeoExpress neoExpress;
        private readonly ContractManifest contractManifest;
        private readonly IHttpClientFactory clientFactory;
        private readonly ILogger<HomeController> logger;
        private readonly RpcClient rpcClient;

        public HomeController(NeoExpress neoExpress, ContractManifest contractManifest, IHttpClientFactory clientFactory, ILogger<HomeController> logger)
        {
            this.neoExpress = neoExpress;
            this.contractManifest = contractManifest;
            this.clientFactory = clientFactory;
            this.logger = logger;

            var http = clientFactory.CreateClient();
            http.BaseAddress = new Uri($"http://localhost:{neoExpress.ConsensusNodes.First().RpcPort}");
            rpcClient = new RpcClient(http);
        }

        public IActionResult Index()
        {
            return RedirectToAction("CreateSale");
        }

        private async Task<SaleViewModel?> GetSaleViewModel(Guid id)
        {
            Script script;
            {
                using var sb = new ScriptBuilder();
                sb.EmitAppCall(contractManifest.Hash, "retrieveSaleInfo", id.ToByteArray());
                script = sb.ToArray();
            }

            var result = rpcClient.InvokeScript(script);
            if (result.State == VMState.HALT 
                && result.Stack.Length > 0
                && result.Stack[0] is Neo.VM.Types.Array array)
            {
                return SaleViewModel.FromStackItem(array);
            }
            
            return null;
        }

        public async Task<IActionResult> SaleInfo(Guid id)
        {
            return View(await GetSaleViewModel(id));
        }

        public IActionResult CreateSale()
        {
            var sale = new CreateSaleViewModel();
            return View(sale);
        }
    
        [HttpPost]
        public async Task<IActionResult> CreateSale([Bind("Description,Price,SaleId")] CreateSaleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); 
            }

            var seller = neoExpress.GetWallet("seller").Default;
            var price = model.Price.ToBigInteger(NativeContract.GAS.Decimals);

            Script script;
            {
                using var sb = new ScriptBuilder();
                sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", seller.ScriptHash, contractManifest.Hash, price * 2);
                sb.EmitAppCall(contractManifest.Hash, "createSale", model.SaleId.ToByteArray(), price, model.Description);
                script = sb.ToArray();
            }

            var signers = new[] { new Signer { Account = seller.ScriptHash, Scopes = WitnessScope.CalledByEntry }};

            var tx = await Task.Run(() => {
                var tm = new TransactionManager(rpcClient, neoExpress.Magic)
                    .MakeTransaction(script, signers)
                    .AddSignature(seller.KeyPair)
                    .AddGas(10)
                    .Sign();
                rpcClient.SendRawTransaction(tm.Tx);
                return tm.Tx;
            });

            ViewBag.TransactionHash = CalculateHash(tx, neoExpress.Magic);
            return View(model);
        }

        public async Task<IActionResult> BuyerDeposit(Guid id)
        {
            var model = await GetSaleViewModel(id);
            if (model == null)
            {
                RedirectToAction("SaleInfo", new { id });
            }

            return View(model);
        }

        [HttpPost]
        [ActionName(nameof(BuyerDeposit))]
        public async Task<IActionResult> BuyerDepositPost(Guid id)
        {
            var model = await GetSaleViewModel(id);
            if (model == null)
            {
                return View(model);
            }

            var buyer = neoExpress.GetWallet("buyer").Default;
            var price = model.Price.ChangeDecimals(NativeContract.GAS.Decimals).Value;

            Script script;
            {
                using var sb = new ScriptBuilder();
                sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", buyer.ScriptHash, contractManifest.Hash, price * 2);
                sb.EmitAppCall(contractManifest.Hash, "buyerDeposit", id.ToByteArray());
                script = sb.ToArray();
            }

            var signers = new[] { new Signer { Account = buyer.ScriptHash, Scopes = WitnessScope.CalledByEntry }};
            var tx = await Task.Run(() => {
                var tm = new TransactionManager(rpcClient, neoExpress.Magic)
                    .MakeTransaction(script, signers)
                    .AddSignature(buyer.KeyPair)
                    .AddGas(10)
                    .Sign();
                rpcClient.SendRawTransaction(tm.Tx);
                return tm.Tx;
            });

            ViewBag.TransactionHash = CalculateHash(tx, neoExpress.Magic);
            return View(model);
        }

        public async Task<IActionResult> ConfirmShipment(Guid id)
        {
            var model = await GetSaleViewModel(id);
            if (model == null)
            {
                RedirectToAction("SaleInfo", new { id });
            }

            return View(model);
        }

        [HttpPost]
        [ActionName(nameof(ConfirmShipment))]
        public async Task<IActionResult> ConfirmShipmentPost(Guid id)
        {
            var model = await GetSaleViewModel(id);
            if (model == null)
            {
                return View(model);
            }

            var seller = neoExpress.GetWallet("seller").Default;

            Script script;
            {
                using var sb = new ScriptBuilder();
                sb.EmitAppCall(contractManifest.Hash, "confirmShipment", id.ToByteArray());
                script = sb.ToArray();
            }

            var signers = new[] { new Signer { Account = seller.ScriptHash, Scopes = WitnessScope.CalledByEntry }};
            var tx = await Task.Run(() => {
                var tm = new TransactionManager(rpcClient, neoExpress.Magic)
                    .MakeTransaction(script, signers)
                    .AddSignature(seller.KeyPair)
                    .AddGas(10)
                    .Sign();
                rpcClient.SendRawTransaction(tm.Tx);
                return tm.Tx;
            });

            ViewBag.TransactionHash = CalculateHash(tx, neoExpress.Magic);
            return View(model);
        }

        public async Task<IActionResult> ConfirmReceived(Guid id)
        {
            var model = await GetSaleViewModel(id);
            if (model == null)
            {
                RedirectToAction("SaleInfo", new { id });
            }

            return View(model);
        }

        [HttpPost]
        [ActionName(nameof(ConfirmReceived))]
        public async Task<IActionResult> ConfirmReceivedPost(Guid id)
        {
            var model = await GetSaleViewModel(id);
            if (model == null)
            {
                return View(model);
            }

            var buyer = neoExpress.GetWallet("buyer").Default;

            Script script;
            {
                using var sb = new ScriptBuilder();
                sb.EmitAppCall(contractManifest.Hash, "confirmReceived", id.ToByteArray());
                script = sb.ToArray();
            }

            var signers = new[] { new Signer { Account = buyer.ScriptHash, Scopes = WitnessScope.CalledByEntry }};
            var tx = await Task.Run(() => {
                var tm = new TransactionManager(rpcClient, neoExpress.Magic)
                    .MakeTransaction(script, signers)
                    .AddSignature(buyer.KeyPair)
                    .AddGas(10)
                    .Sign();
                rpcClient.SendRawTransaction(tm.Tx);
                return tm.Tx;
            });

            ViewBag.TransactionHash = CalculateHash(tx, neoExpress.Magic);
            return View(model);
        }

        public async Task<IActionResult> AppLog(string id)
        {
            var json = await GetAppLog();
            ViewBag.TxHash = id;

            return View(json);            

            Task<Neo.IO.Json.JObject> GetAppLog()
            {
                return Task.Run<Neo.IO.Json.JObject>(() => {
                    try
                    {
                        return rpcClient.RpcSend("getapplicationlog", id.ToString());
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                });
            }
        }

        public async Task<IActionResult> Balance(string id)
        {
            var wallet = neoExpress.GetWallet(id);

            var nep5 = new Nep5API(rpcClient);
            var gasBalance = new BigDecimal(
                nep5.BalanceOf(NativeContract.GAS.Hash, wallet.Default.ScriptHash), 
                NativeContract.GAS.Decimals);

            return View((wallet, gasBalance));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // TODO: remove CalculateHash when https://github.com/neo-project/neo/pull/1902 merges 
        static UInt256 CalculateHash(IVerifiable verifiable, uint magic)
        {
            return new UInt256(Neo.Cryptography.Crypto.Hash256(GetHashData(verifiable, magic)));

            static byte[] GetHashData(IVerifiable verifiable, uint magic)
            {
                using var ms = new System.IO.MemoryStream();
                using var writer = new System.IO.BinaryWriter(ms);
                writer.Write(magic);
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}
