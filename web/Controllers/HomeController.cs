using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using SafePuchaseWeb.Models;

namespace SafePuchaseWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly RpcClient rpcClient = new RpcClient("http://localhost:49332");

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

        [HttpPost]
        public async Task<IActionResult> CreateSale([Bind("Description,Price,SaleId")] CreateSaleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); 
            }

            var seller = "NajmT8yKhtCcM48K8eCo8daWFtdgCTfmuR".ToScriptHash();
            var contract = Neo.UInt160.Parse("0xbb2d2ebee39cfa1edcf94b655e9d9630672d0e6a");
            var price = model.Price.ToBigInteger(NativeContract.GAS.Decimals);

            Script script;
            {
                using var sb = new ScriptBuilder();
                sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", seller, contract, price * 2);
                // sb.EmitAppCall(contract, "createSale", model.SaleId.ToByteArray(), price, model.Description);
                script = sb.ToArray();
            }

            var keyPair = new KeyPair("b6e3d15d08dfd11cc43aca3c7bdb029d86c10014369749a300b9b3e9b5fef790".HexToBytes());
            var signers = new[] { new Signer { Account = seller, Scopes = WitnessScope.CalledByEntry }};

            // var tx = await Task.Run(() => {
                var tm = new TransactionManager(rpcClient)
                    .MakeTransaction(script, signers)
                    .AddSignature(keyPair)
                    .Sign();
                // rpcClient.SendRawTransaction(tm.Tx);
                // return ;
            // });

            model.TransactionHash = tm.Tx.Hash;
            return View(model);
        }

        public IActionResult BuyerDeposit()
        {
            return View();
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
            var json = new Neo.IO.Json.JObject();
            json["question"] = "what is 6 x 9";
            json["answer"] = 42;


            // await Task.Run(() => rpcClient.RpcSend("getapplicationlog", id.ToString()));
            ViewBag.TxHash = id;

            return View(json);            
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
