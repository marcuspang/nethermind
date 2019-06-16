/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System.Numerics;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.DataMarketplace.Core.Domain;

namespace Nethermind.DataMarketplace.Infrastructure.Rpc.Models
{
    public class DataRequestForRpc
    {
        public Keccak DataHeaderId { get; set; }
        public uint Units { get; set; }
        public BigInteger Value { get; set; }
        public uint ExpiryTime { get; set; }
        public Address Provider { get; set; }

        public DataRequestForRpc()
        {
        }

        public DataRequestForRpc(DataRequest dataRequest)
        {
            DataHeaderId = dataRequest.DataHeaderId;
            Units = dataRequest.Units;
            Value = dataRequest.Value;
            ExpiryTime = dataRequest.ExpiryTime;
            Provider = dataRequest.Provider;
        }
    }
}