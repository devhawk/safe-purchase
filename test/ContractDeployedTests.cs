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

[CheckpointPath("checkpoints/1-contract-deployed.neoxp-checkpoint")]
public class ContractDeployedTests : IClassFixture<CheckpointFixture<ContractDeployedTests>>
{
    readonly CheckpointFixture fixture;
    readonly ExpressChain chain;

    public ContractDeployedTests(CheckpointFixture<ContractDeployedTests> fixture)
    {
        this.fixture = fixture;
        this.chain = fixture.FindChain();
    }

    [Fact]
    public void contract_owner_in_storage()
    {
        var settings = chain.GetProtocolSettings();
        var owner = chain.GetDefaultAccount("owner").ToScriptHash(settings.AddressVersion);

        using var snapshot = fixture.GetSnapshot();

        var storages = snapshot.GetContractStorages<SafePurchase>();
        storages.Count().Should().Be(1);
        storages.TryGetValue(new byte[] { 0xff }, out var item).Should().BeTrue();
        item!.Should().Be(owner);
    }

    [Fact]
    public void seller_has_10000_neo()
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
        engine.ResultStack.Peek(0).Should().BeEquivalentTo(10000);
    }
}

