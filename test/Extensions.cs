using System;
using System.Linq.Expressions;
using Neo;
using Neo.BlockchainToolkit;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using NeoTestHarness;

namespace DevHawk.ContractTests;

// TODO: remove these after Neo test 3.2 ships

public static class NativeContracts
{
    static Lazy<UInt160> neoToken = new Lazy<UInt160>(() => UInt160.Parse("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5"));
    public static UInt160 NeoToken => neoToken.Value;

    static Lazy<UInt160> gasToken = new Lazy<UInt160>(() => UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf"));
    public static UInt160 GasToken => gasToken.Value;
}

static class Extensions
{
    public static void EmitContractCall<T>(this ScriptBuilder builder, DataCache snapshot, Expression<Action<T>> expression)
        where T : class
    {
        var scriptHash = snapshot.GetContractScriptHash<T>();
        EmitContractCall<T>(builder, scriptHash, expression);
    }

    public static void EmitContractCall<T>(this ScriptBuilder builder, UInt160 scriptHash, Expression<Action<T>> expression)
    {
        var methodCall = (MethodCallExpression)expression.Body;
        var operation = methodCall.Method.Name;

        for (var x = methodCall.Arguments.Count - 1; x >= 0; x--)
        {
            var obj = Expression.Lambda(methodCall.Arguments[x]).Compile().DynamicInvoke();
            var param = ContractParameterParser.ConvertObject(obj);
            builder.EmitPush(param);
        }
        builder.EmitPush(methodCall.Arguments.Count);
        builder.Emit(OpCode.PACK);
        builder.EmitPush(CallFlags.All);
        builder.EmitPush(operation);
        builder.EmitPush(scriptHash);
        builder.EmitSysCall(ApplicationEngine.System_Contract_Call);
    }
}

public interface Nep17Token
{
    System.Numerics.BigInteger balanceOf(Neo.UInt160 account);
    System.Numerics.BigInteger decimals();
    string symbol();
    System.Numerics.BigInteger totalSupply();
    bool transfer(Neo.UInt160 @from, Neo.UInt160 to, System.Numerics.BigInteger amount, object data);

    interface Events
    {
        void Transfer(Neo.UInt160 @from, Neo.UInt160 to, System.Numerics.BigInteger amount);
    }
}