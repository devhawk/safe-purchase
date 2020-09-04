using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly RpcClient rpcClient = new RpcClient("http://localhost:49332");

        private readonly NeoExpress neoExpress;
        private readonly ContractManifest contractManifest;
        private readonly ILogger<HomeController> logger;

        public HomeController(NeoExpress neoExpress, ContractManifest contractManifest, ILogger<HomeController> logger)
        {
            this.neoExpress = neoExpress;
            this.contractManifest = contractManifest;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CreateSale()
        {
            var sale = new CreateSaleViewModel();
            return View(sale);
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

            model.TransactionHash = CalculateHash(tx, neoExpress.Magic);
            return View(model);
        }

        public IActionResult BuyerDeposit(Guid id)
        {
            return View(id);
        }

        public IActionResult ConfirmShipment()
        {
            return View();
        }

        public IActionResult ConfirmReceived()
        {
            return View();
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
