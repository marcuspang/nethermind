// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Serialization.Rlp;
using Nethermind.Trie;
using Nethermind.Int256;
using Nethermind.Core.Extensions;
using Nethermind.Db;
using Nethermind.State;

namespace Nethermind.VerkleTransition.Cli;

public class VerkleTreeMigrator : ITreeVisitor<TreePathContext>
{
    public readonly VerkleStateTree _verkleStateTree;
    private readonly IStateReader _stateReader;
    private readonly IDb _preImageDb;
    private Address? _lastAddress;
    private Account? _lastAccount;
    private int _leafNodeCounter = 0;

    private const int StateTreeCommitThreshold = 1000;

    public VerkleTreeMigrator(VerkleStateTree verkleStateTree, IStateReader stateReader, IDb preImageDb)
    {
        _verkleStateTree = verkleStateTree;
        _stateReader = stateReader;
        _preImageDb = preImageDb;
    }

    public bool IsFullDbScan => true;

    public bool ShouldVisit(in TreePathContext ctx, Hash256 nextNode)
    {
        return true;
    }

    public void VisitTree(in TreePathContext nodeContext, Hash256 rootHash, TrieVisitContext trieVisitContext)
    {
        Console.WriteLine($"Starting migration from Merkle tree with root: {rootHash}");
        _lastAddress = null;
        _lastAccount = null;
    }

    public void VisitMissingNode(in TreePathContext nodeContext, Hash256 nodeHash, TrieVisitContext trieVisitContext)
    {
        Console.WriteLine($"Warning: Missing node encountered: {nodeHash}");
    }

    public void VisitBranch(in TreePathContext nodeContext, TrieNode node, TrieVisitContext trieVisitContext)
    {
    }

    public void VisitExtension(in TreePathContext nodeContext, TrieNode node, TrieVisitContext trieVisitContext)
    {
    }

    private readonly AccountDecoder decoder = new();

    public void VisitLeaf(in TreePathContext nodeContext, TrieNode node, TrieVisitContext trieVisitContext, ReadOnlySpan<byte> value)
    {
        TreePath path = nodeContext.Path.Append(node.Key);

        if (!trieVisitContext.IsStorage)
        {
            byte[]? nodeValueBytes = node.Value.ToArray();
            if (nodeValueBytes is null)
            {
                return;
            }
            Account? account = decoder.Decode(new RlpStream(nodeValueBytes));
            if (account is null)
            {
                return;
            }

            // Reconstruct the full keccak hash
            byte[]? addressBytes = RetrievePreimage(path.Path.BytesAsSpan);
            if (addressBytes is not null)
            {
                var address = new Address(addressBytes);

                // Update code size if account has code
                if (account.HasCode)
                {
                    byte[]? code = _stateReader.GetCode(account.CodeHash);
                    if (code is not null)
                    {
                        account.CodeSize = (UInt256)code.Length;
                        MigrateContractCode(address, code);
                    }
                }
                MigrateAccount(address, account);

                _lastAddress = address;
                _lastAccount = account;
            }
        }
        else
        {
            if (_lastAddress is null || _lastAccount is null)
            {
                Console.WriteLine($"No address or account detected for storage node: {node}");
                return;
            }
            // Reconstruct the full keccak hash
            byte[]? storageSlotBytes = RetrievePreimage(path.Path.BytesAsSpan);
            if (storageSlotBytes is null)
            {
                Console.WriteLine($"Storage slot is null for node: {node} with key: {path.Path.BytesAsSpan.ToHexString()}");
                return;
            }
            UInt256 storageSlot = new(storageSlotBytes);
            byte[] storageValue = value.ToArray();
            MigrateAccountStorage(_lastAddress, storageSlot, storageValue);
        }

        CommitIfThresholdReached();
    }

    private void CommitIfThresholdReached()
    {
        _leafNodeCounter++;
        if (_leafNodeCounter >= StateTreeCommitThreshold)
        {
            _verkleStateTree.Commit();
            _leafNodeCounter = 0;
        }
    }


    public void VisitCode(in TreePathContext nodeContext, Hash256 codeHash, TrieVisitContext trieVisitContext) { }

    private static byte[]? RetrievePreimage(Span<byte> key)
    {
        // TODO: return first 20 bytes until preimage db is implemented
        return key[..20].ToArray();
        // return _preImageDb.Get(key);
    }


    private void MigrateAccount(Address address, Account account)
    {
        _verkleStateTree.Set(address, account);
    }

    private void MigrateContractCode(Address address, byte[] code)
    {
        _verkleStateTree.SetCode(address, code);
    }

    private void MigrateAccountStorage(Address address, UInt256 index, byte[] value)
    {
        var storageKey = new StorageCell(address, index);
        _verkleStateTree.SetStorage(storageKey, value);
    }

    public void FinalizeMigration(long blockNumber)
    {
        // Commit any remaining changes
        if (_leafNodeCounter > 0)
        {
            _verkleStateTree.Commit();
        }
        _verkleStateTree.CommitTree(blockNumber);
        Console.WriteLine($"Migration completed");
    }
}
