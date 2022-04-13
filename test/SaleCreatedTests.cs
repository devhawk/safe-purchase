using System.Linq;
using FluentAssertions;
using Neo.Assertions;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Neo.BlockchainToolkit.SmartContract;
using Neo.VM;
using NeoTestHarness;
using Xunit;

namespace DevHawk.ContractTests;

[CheckpointPath("checkpoints/2-create-sale.neoxp-checkpoint")]
public class SaleCreatedTests : IClassFixture<CheckpointFixture<SaleCreatedTests>>
{
    readonly CheckpointFixture fixture;
    readonly ExpressChain chain;

    public SaleCreatedTests(CheckpointFixture<SaleCreatedTests> fixture)
    {
        this.fixture = fixture;
        this.chain = fixture.FindChain();
    }

    [Fact]
    public void seller_has_9900_neo()
    {
        var settings = chain.GetProtocolSettings();
        var seller = chain.GetDefaultAccount("seller").ToScriptHash(settings.AddressVersion);

        using var builder = new ScriptBuilder();
        builder.EmitContractCall<Nep17Token>(NativeContracts.NeoToken, c => c.balanceOf(seller));

        using var snapshot = fixture.GetSnapshot();
        using var engine = new TestApplicationEngine(snapshot, settings, seller);
        engine.ExecuteScript(builder.ToArray());

        engine.State.Should().Be(VMState.HALT);
        engine.ResultStack.Should().HaveCount(1);
        engine.ResultStack.Peek(0).Should().BeEquivalentTo(9900);
    }

    [Fact]
    public void seller_has_one_sale_listed()
    {
        var settings = chain.GetProtocolSettings();
        var seller = chain.GetDefaultAccount("seller").ToScriptHash(settings.AddressVersion);

        using var snapshot = fixture.GetSnapshot();
        using var builder = new ScriptBuilder();
        builder.EmitContractCall<SafePurchase>(snapshot, c => c.sales(seller));

        using var engine = new TestApplicationEngine(snapshot, settings, seller);
        engine.ExecuteScript(builder.ToArray());

        engine.State.Should().Be(VMState.HALT);
        engine.ResultStack.Should().HaveCount(1);
        var result = engine.ResultStack.Pop<Neo.VM.Types.Map>();
        result.Count.Should().Be(1);
    }
}

