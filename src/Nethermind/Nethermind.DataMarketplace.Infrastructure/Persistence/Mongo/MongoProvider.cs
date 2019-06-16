using System.Collections.Generic;
using System.Numerics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Logging;
using Nethermind.Dirichlet.Numerics;

namespace Nethermind.DataMarketplace.Infrastructure.Persistence.Mongo
{
    public class MongoProvider : IMongoProvider
    {
        private static bool _initialized;
        private static IMongoClient _client;
        private readonly IMongoConfig _config;
        private readonly ILogger _logger;

        public MongoProvider(IMongoConfig config, ILogManager logManager)
        {
            _config = config;
            _logger = logManager.GetClassLogger();
            if (_initialized)
            {
                return;
            }

            RegisterConventions();
            var connectionUrl = new MongoUrl(config.ConnectionString);
            var clientSettings = MongoClientSettings.FromUrl(connectionUrl);
            if (_config.LogQueries)
            {
                clientSettings.ClusterConfigurator = cb =>
                {
                    cb.Subscribe<CommandStartedEvent>(e =>
                    {
                        if (_logger.IsInfo) _logger.Info($"MongoDB command started '{e.CommandName}': {e.Command.ToJson()}");
                    });   
                    cb.Subscribe<CommandSucceededEvent>(e =>
                    {
                        if (_logger.IsInfo) _logger.Info($"MongoDB command succeeded '{e.CommandName}': {e.Reply.ToJson()}");
                    }); 
                    cb.Subscribe<CommandFailedEvent>(e =>
                    {
                        if (_logger.IsError) _logger.Error($"MongoDB command failed '{e.CommandName}': {e.Failure}");
                    }); 
                };
            }

            _client = new MongoClient(clientSettings);
            _initialized = true;
        }
        
        public IMongoDatabase GetDatabase() => _client.GetDatabase(_config.Database);

        private static void RegisterConventions()
        {
            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(typeof(Address), new AddressSerializer());
            BsonSerializer.RegisterSerializer(typeof(BigInteger), new BigIntegerSerializer());
            BsonSerializer.RegisterSerializer(typeof(Keccak), new KeccakSerializer());
            BsonSerializer.RegisterSerializer(typeof(PublicKey), new PublicKeySerializer());
            BsonSerializer.RegisterSerializer(typeof(Signature), new SignatureSerializer());
            BsonSerializer.RegisterSerializer(typeof(UInt256), new UInt256Serializer());
            ConventionRegistry.Register("Conventions", new MongoDbConventions(), _ => true);
        }

        private class MongoDbConventions : IConventionPack
        {
            public IEnumerable<IConvention> Conventions => new List<IConvention>
            {
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String),
                new CamelCaseElementNameConvention()
            };
        }

        private class AddressSerializer : SerializerBase<Address>
        {
            public override Address Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var value = context.Reader.ReadString();

                return string.IsNullOrWhiteSpace(value) ? null : new Address(value);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Address value)
                => context.Writer.WriteString(value?.ToString());
        }

        private class BigIntegerSerializer : SerializerBase<BigInteger>
        {
            public override BigInteger Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
                => BigInteger.Parse(context.Reader.ReadString());

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args,
                BigInteger value) => context.Writer.WriteString(value.ToString());
        }

        private class KeccakSerializer : SerializerBase<Keccak>
        {
            public override Keccak Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var value = context.Reader.ReadString();

                return string.IsNullOrWhiteSpace(value) ? null : new Keccak(value);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Keccak value)
                => context.Writer.WriteString(value?.ToString() ?? string.Empty);
        }
        
        private class PublicKeySerializer : SerializerBase<PublicKey>
        {
            public override PublicKey Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var value = context.Reader.ReadString();

                return string.IsNullOrWhiteSpace(value) ? null : new PublicKey(value);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args,
                PublicKey value) => context.Writer.WriteString(value?.ToString() ?? string.Empty);
        }

        private class SignatureSerializer : SerializerBase<Signature>
        {
            public override Signature Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var value = context.Reader.ReadString();

                return string.IsNullOrWhiteSpace(value) ? null : new Signature(value);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args,
                Signature value) => context.Writer.WriteString(value?.ToString() ?? string.Empty);
        }

        private class UInt256Serializer : SerializerBase<UInt256>
        {
            public override UInt256 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
                => UInt256.Parse(context.Reader.ReadString());

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UInt256 value)
                => context.Writer.WriteString(value.ToString());
        }
    }
}