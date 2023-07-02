using System.Reflection;
using MessagePack;
using MessagePack.Resolvers;
using SocketIO.Core;
using SocketIO.Serializer.Core;
using SocketIO.Serializer.Tests.Models;

namespace SocketIO.Serializer.MessagePack.Tests;

public class SocketIOMessagePackSerializerTests
{
    private static IEnumerable<(
            string eventName,
            string ns,
            object[] data,
            IEnumerable<SerializedItem> expectedItems)>
        SerializeTupleCases =>
        new (string eventName, string ns, object[] data, IEnumerable<SerializedItem> expectedItems)[]
        {
            (
                "test",
                string.Empty,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/",
                            Data = new List<object> { "test" }
                        })
                    }
                }),
            (
                "test",
                string.Empty,
                new object[1],
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/",
                            Data = new List<object> { "test", null! }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object> { "test" }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                new object[] { true },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object> { "test", true }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                new object[] { true, false, 123 },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object> { "test", true, false, 123 }
                        })
                    }
                }),
            (
                "test",
                string.Empty,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/",
                            Data = new List<object>
                            {
                                "test",
                                new byte[] { 1, 2, 3 }
                            }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                new object[]
                {
                    new byte[] { 1, 2, 3 },
                    new byte[] { 4, 5, 6 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object>
                            {
                                "test",
                                new byte[] { 1, 2, 3 },
                                new byte[] { 4, 5, 6 }
                            }
                        })
                    }
                }),
        };

    public static IEnumerable<object[]> SerializeCases => SerializeTupleCases
        .Select((x, caseId) => new object[]
        {
            caseId,
            x.eventName,
            x.ns,
            x.data,
            x.expectedItems
        });

    [Theory]
    [MemberData(nameof(SerializeCases))]
    public void Should_serialize_given_event_message(
        int caseId,
        string eventName,
        string ns,
        object[] data,
        IEnumerable<SerializedItem> expectedItems)
    {
        var serializer = new SocketIOMessagePackSerializer();
        var items = serializer.Serialize(eventName, ns, data);
        items.Should().BeEquivalentTo(expectedItems, config => config.WithStrictOrdering());
    }

    private static IEnumerable<(
            string? ns,
            EngineIO eio,
            object? auth,
            IEnumerable<KeyValuePair<string, string>> queries,
            SerializedItem? expected)>
        SerializeConnectedTupleCases =>
        new (
            string? ns,
            EngineIO eio,
            object? auth,
            IEnumerable<KeyValuePair<string, string>> queries,
            SerializedItem? expected)[]
            {
                // https://msgpack.solder.party/
                (null, EngineIO.V3, null, null, null)!,
                (null, EngineIO.V3, new { userId = 1 }, null, null)!,
                (null, EngineIO.V3, new { userId = 1 }, new Dictionary<string, string>
                {
                    ["hello"] = "world"
                }, null),
                ("/test", EngineIO.V3, null, null, new()
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"nsp":"/test"}
                    Binary = new byte[]
                    {
                        0x82, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73,
                        0x74
                    }
                })!,
                ("/test", EngineIO.V3, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        // {"type":0,"query":"key=value","nsp":"/test?key=value"}
                        Binary = new byte[]
                        {
                            0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA5, 0x71, 0x75, 0x65, 0x72, 0x79, 0xA9, 0x6B,
                            0x65, 0x79, 0x3D, 0x76, 0x61, 0x6C, 0x75, 0x65, 0xA3, 0x6E, 0x73, 0x70, 0xAF, 0x2F, 0x74,
                            0x65, 0x73, 0x74, 0x3F, 0x6B, 0x65, 0x79, 0x3D, 0x76, 0x61, 0x6C, 0x75, 0x65
                        }
                    }),
                (null, EngineIO.V4, null, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":null,"nsp":"/"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0xC0, 0xA3, 0x6E, 0x73,
                        0x70, 0xA1, 0x2F
                    }
                })!,
                ("/test", EngineIO.V4, null, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":null,"nsp":"/test"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0xC0, 0xA3, 0x6E, 0x73,
                        0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73, 0x74
                    }
                })!,
                (null, EngineIO.V4, new { userId = 1 }, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":{"userId":1},"nsp":"/"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x81, 0xA6, 0x75, 0x73,
                        0x65, 0x72, 0x49, 0x64, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F
                    }
                })!,
                ("/test", EngineIO.V4, new { userId = 1 }, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":{"userId":1},"nsp":"/test"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x81, 0xA6, 0x75, 0x73,
                        0x65, 0x72, 0x49, 0x64, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73, 0x74
                    }
                })!,
                (null, EngineIO.V4, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new SerializedItem
                    {
                        Type = SerializedMessageType.Binary,
                        // {"type":0,"data":null,"nsp":"/"}
                        Binary = new byte[]
                        {
                            0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0xC0, 0xA3, 0x6E,
                            0x73, 0x70, 0xA1, 0x2F
                        }
                    }),
            };

    public static IEnumerable<object?[]> SerializeConnectedCases => SerializeConnectedTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.ns,
            x.eio,
            x.auth,
            x.queries,
            x.expected
        });

    [Theory]
    [MemberData(nameof(SerializeConnectedCases))]
    public void Should_serialize_connected_messages(
        int caseId,
        string? ns,
        EngineIO eio,
        object auth,
        IEnumerable<KeyValuePair<string, string>> queries,
        SerializedItem? expected)
    {
        var serializer = new SocketIOMessagePackSerializer();
        serializer.SerializeConnectedMessage(ns, eio, auth, queries)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(EngineIO eio, string? text, object? expected)> DeserializeTextTupleCases =>
        new (EngineIO eio, string? text, object? expected)[]
        {
            (EngineIO.V3, string.Empty, null),
            (EngineIO.V3, "hello", null),
            (EngineIO.V4, "2", new { Type = MessageType.Ping }),
            (EngineIO.V4, "3", new { Type = MessageType.Pong }),
            (
                EngineIO.V4,
                "0{\"sid\":\"wOuAvDB9Jj6yE0VrAL8N\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}",
                new
                {
                    Type = MessageType.Opened,
                    Sid = "wOuAvDB9Jj6yE0VrAL8N",
                    PingInterval = 25000,
                    PingTimeout = 30000,
                    Upgrades = new List<string> { "websocket" }
                }),
            (EngineIO.V3, "4{\"type\":0,\"nsp\":\"/\"}", new { Type = MessageType.Connected }),
            (EngineIO.V3, "4{\"type\":0,\"nsp\":\"/test\"}", new { Type = MessageType.Connected, Namespace = "/test" }),
            (
                EngineIO.V3,
                "4{\"type\":0,\"query\":\"token=eio3\",\"nsp\":\"/test?token=eio3\"}",
                new
                {
                    Type = MessageType.Connected,
                    Namespace = "/test?token=eio3"
                }),
            (
                EngineIO.V4,
                "4{\"type\":0,\"data\":{\"sid\":\"test-id\"}}",
                new
                {
                    Type = MessageType.Connected,
                    Sid = "test-id"
                }),
            (
                EngineIO.V4,
                "4{\"type\":0,\"data\":{\"sid\":\"test-id\"},\"nsp\":\"/test\"}",
                new
                {
                    Type = MessageType.Connected,
                    Sid = "test-id",
                    Namespace = "/test"
                }),
            (
                EngineIO.V3,
                "4{\"type\":4,\"data\":\"Authentication error\",\"nsp\":\"/\"}",
                new
                {
                    Type = MessageType.Error,
                    Error = "Authentication error"
                }),
        };

    public static IEnumerable<object?[]> DeserializeTextCases => DeserializeTextTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.eio,
            x.text,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeTextCases))]
    public void Should_deserialize_text(
        int caseId,
        EngineIO eio,
        string? text,
        object expected)
    {
        var serializer = new SocketIOMessagePackSerializer();
        serializer.Deserialize(eio, text)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(EngineIO eio, byte[] binary, object? expected)> DeserializeBinaryTupleCases =>
        new (EngineIO eio, byte[] binary, object? expected)[]
        {
            (
                EngineIO.V4,
                // {"type":1,"nsp":"/"}
                new byte[] { 0x82, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F },
                new
                {
                    Type = MessageType.Disconnected,
                    Namespace = "/"
                }),
            (
                EngineIO.V4,
                // {"type":1,"nsp":"/test"}
                new byte[]
                {
                    0x82, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73, 0x74
                },
                new
                {
                    Type = MessageType.Disconnected,
                    Namespace = "/test"
                }),
            (
                EngineIO.V4,
                // {"type":2,"data":["hi","socket.io"],"nsp":"/"}
                new byte[]
                {
                    0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x02, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x92, 0xA2, 0x68, 0x69,
                    0xA9, 0x73, 0x6F, 0x63, 0x6B, 0x65, 0x74, 0x2E, 0x69, 0x6F, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F
                },
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Data = new object[] { "socket.io" },
                    Namespace = "/"
                }),
            (
                EngineIO.V4,
                // {"type":2,"data":["hi","socket.io"],"nsp":"/nsp"}
                new byte[]
                {
                    0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x02, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x92, 0xA2, 0x68, 0x69,
                    0xA9, 0x73, 0x6F, 0x63, 0x6B, 0x65, 0x74, 0x2E, 0x69, 0x6F, 0xA3, 0x6E, 0x73, 0x70, 0xA4, 0x2F,
                    0x6E, 0x73, 0x70
                },
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Data = new object[] { "socket.io" },
                    Namespace = "/nsp"
                }),
            (
                EngineIO.V4,
                // {"type":2,"data":["hi","socket.io"],"id":17,"nsp":"/"}
                new byte[]
                {
                    0x84, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x02, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x92, 0xA2, 0x68, 0x69,
                    0xA9, 0x73, 0x6F, 0x63, 0x6B, 0x65, 0x74, 0x2E, 0x69, 0x6F, 0xA2, 0x69, 0x64, 0x11, 0xA3, 0x6E,
                    0x73, 0x70, 0xA1, 0x2F
                },
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Data = new object[] { "socket.io" },
                    Namespace = "/",
                    Id = 17
                }),
            (
                EngineIO.V4,
                // {"type":2,"data":["hi","socket.io"],"id":17,"nsp":"/nsp"}
                new byte[]
                {
                    0x84, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x02, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x92, 0xA2, 0x68, 0x69,
                    0xA9, 0x73, 0x6F, 0x63, 0x6B, 0x65, 0x74, 0x2E, 0x69, 0x6F, 0xA2, 0x69, 0x64, 0x11, 0xA3, 0x6E,
                    0x73, 0x70, 0xA4, 0x2F, 0x6E, 0x73, 0x70
                },
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Data = new object[] { "socket.io" },
                    Namespace = "/nsp",
                    Id = 17
                }),
            (
                EngineIO.V4,
                // {"type":3,"data":["hello"],"id":1,"nsp":"/"}
                new byte[]
                {
                    0x84, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x03, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x91, 0xA5, 0x68, 0x65,
                    0x6C, 0x6C, 0x6F, 0xA2, 0x69, 0x64, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F
                },
                new
                {
                    Type = MessageType.Ack,
                    Data = new object[] { "hello" },
                    Namespace = "/",
                    Id = 1
                }),
            (
                EngineIO.V4,
                new byte[]
                {
                    0x84, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x03, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x91, 0xA5, 0x68, 0x65,
                    0x6C, 0x6C, 0x6F, 0xA2, 0x69, 0x64, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA4, 0x2F, 0x6E, 0x73, 0x70
                },
                new
                {
                    Type = MessageType.Ack,
                    Data = new object[] { "hello" },
                    Namespace = "/nsp",
                    Id = 1
                }),
        };

    public static IEnumerable<object?[]> DeserializeBinaryCases => DeserializeBinaryTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.eio,
            x.binary,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeBinaryCases))]
    public void Should_deserialize_binary(
        int caseId,
        EngineIO eio,
        byte[] binary,
        object expected)
    {
        var serializer = new SocketIOMessagePackSerializer();
        serializer.Deserialize(eio, binary)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        IMessage2 message,
        int index,
        MessagePackSerializerOptions? options,
        object expected)> DeserializeGenericMethodTupleCases =>
        new (IMessage2 message, int index, MessagePackSerializerOptions? AssertionOptions, object expected)[]
        {
            (new PackMessage(MessageType.Event)
            {
                Event = "event",
                Data = new List<object> { 1 }
            }, 0, null, 1)!,
            (new PackMessage(MessageType.Event)
            {
                Event = "event",
                Data = new List<object> { "hello" }
            }, 0, null, "hello")!,
            (
                new PackMessage(MessageType.Event)
                {
                    Event = "event",
                    Data = new List<object>
                    {
                        "hello",
                        new
                        {
                            User = "admin",
                            Password = "test"
                        }
                    }
                }, 1, null, new MessagePackUserPasswordDto
                {
                    User = "admin",
                    Password = "test"
                }),
            (
                new PackMessage(MessageType.Event)
                {
                    Event = "event",
                    Data = new List<object>
                    {
                        "hello",
                        new
                        {
                            User = "admin",
                            Password = "test"
                        }
                    }
                }, 1, ContractlessStandardResolver.Options, new UserPasswordDto
                {
                    User = "admin",
                    Password = "test"
                }),
            (
                new PackMessage(MessageType.Event)
                {
                    Event = "event",
                    Data = new List<object>
                    {
                        "hello world!"u8.ToArray(),
                        new
                        {
                            Size = 2023,
                            Name = "test.txt",
                            Bytes = "🐮🍺"u8.ToArray()
                        }
                    }
                }, 0, ContractlessStandardResolver.Options, "hello world!"u8.ToArray()),
            (
                new PackMessage(MessageType.Event)
                {
                    Event = "event",
                    Data = new List<object>
                    {
                        "hello world!"u8.ToArray(),
                        new
                        {
                            Size = 2023,
                            Name = "test.txt",
                            Bytes = "🐮🍺"u8.ToArray()
                        }
                    }
                }, 1, ContractlessStandardResolver.Options, new FileDto
                {
                    Size = 2023,
                    Name = "test.txt",
                    Bytes = "🐮🍺"u8.ToArray()
                }),
        };

    public static IEnumerable<object?[]> DeserializeGenericMethodCases => DeserializeGenericMethodTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.message,
            x.index,
            x.options,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeGenericMethodCases))]
    public void Should_deserialize_generic_type_by_message_and_index(
        int caseId,
        IMessage2 message,
        int index,
        MessagePackSerializerOptions? options,
        object expected)
    {
        var serializer = new SocketIOMessagePackSerializer(options);
        var actual = serializer.GetType()
            .GetMethod(
                nameof(SocketIOMessagePackSerializer.Deserialize),
                BindingFlags.Public | BindingFlags.Instance,
                new[] { typeof(IMessage2), typeof(int) })!
            .MakeGenericMethod(expected.GetType())
            .Invoke(serializer, new object?[] { message, index });
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(DeserializeGenericMethodCases))]
    public void Should_deserialize_non_generic_type_by_message_and_index_and_type(
        int caseId,
        IMessage2 message,
        int index,
        MessagePackSerializerOptions? options,
        object expected)
    {
        var serializer = new SocketIOMessagePackSerializer(options);
        var actual = serializer.Deserialize(message, index, expected.GetType());
        actual.Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        int packetId,
        string? nsp,
        MessagePackSerializerOptions? options,
        object[] data,
        List<SerializedItem> expected)> SerializePacketIdNamespaceDataTupleCases =>
        new (
            int packetId,
            string? nsp,
            MessagePackSerializerOptions? options,
            object[] data,
            List<SerializedItem> expected)[]
            {
                (0, null, null, null,
                    new()
                    {
                        new()
                        {
                            Type = SerializedMessageType.Binary,
                            // {"type":3,"data":[],"options":{"compress":true},"id":0,"nsp":"/"}
                            Binary = new byte[]
                            {
                                0x85, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x03, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x90, 0xA7,
                                0x6F, 0x70, 0x74, 0x69, 0x6F, 0x6E, 0x73, 0x81, 0xA8, 0x63, 0x6F, 0x6D, 0x70, 0x72,
                                0x65, 0x73, 0x73, 0xC3, 0xA2, 0x69, 0x64, 0x00, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F
                            }
                        }
                    })!,
                (0, null, null, new object?[] { "string", 1, true, null },
                    new()
                    {
                        new()
                        {
                            Type = SerializedMessageType.Binary,
                            // {"type":6,"data":["string",1,true,null],"options":{"compress":true},"id":0,"nsp":"/"}
                            Binary = new byte[]
                            {
                                0x85, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x06, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x94, 0xA6,
                                0x73, 0x74, 0x72, 0x69, 0x6E, 0x67, 0xD2, 0x00, 0x00, 0x00, 0x01, 0xC3, 0xC0, 0xA7,
                                0x6F, 0x70, 0x74, 0x69, 0x6F, 0x6E, 0x73, 0x81, 0xA8, 0x63, 0x6F, 0x6D, 0x70, 0x72,
                                0x65, 0x73, 0x73, 0xC3, 0xA2, 0x69, 0x64, 0x00, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F
                            }
                        }
                    })!,
                (23, "/test", null, new object?[] { "string", 1, true, null },
                    new()
                    {
                        new()
                        {
                            Type = SerializedMessageType.Binary,
                            // {"type":6,"data":["string",1,true,null],"options":{"compress":true},"id":23,"nsp":"/test"}
                            Binary = new byte[]
                            {
                                0x85, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x06, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x94, 0xA6,
                                0x73, 0x74, 0x72, 0x69, 0x6E, 0x67, 0xD2, 0x00, 0x00, 0x00, 0x01, 0xC3, 0xC0, 0xA7,
                                0x6F, 0x70, 0x74, 0x69, 0x6F, 0x6E, 0x73, 0x81, 0xA8, 0x63, 0x6F, 0x6D, 0x70, 0x72,
                                0x65, 0x73, 0x73, 0xC3, 0xA2, 0x69, 0x64, 0x17, 0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F,
                                0x74, 0x65, 0x73, 0x74
                            }
                        }
                    })!,
                (8964, "/test", ContractlessStandardResolver.Options,
                    new object?[]
                    {
                        123456.789,
                        new UserPasswordDto
                        {
                            User = "test",
                            Password = "hello"
                        },
                        new FileDto
                        {
                            Size = 2023,
                            Name = "test.txt",
                            Bytes = "🐮🍺"u8.ToArray()
                        }
                    },
                    new()
                    {
                        new()
                        {
                            Type = SerializedMessageType.Binary,
                            // {"type":6,"data":[123456.789,{"User":"test","Password":"hello"},{"Size":2023,"Name":"test.txt","Bytes":{"type":"Buffer","data":[240,159,144,174,240,159,141,186]}}],"options":{"compress":true},"id":8964,"nsp":"/test"}
                            Binary = new byte[]
                            {
                                0x85, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x06, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x93, 0xCB,
                                0x40, 0xFE, 0x24, 0x0C, 0x9F, 0xBE, 0x76, 0xC9, 0x82, 0xA4, 0x55, 0x73, 0x65, 0x72,
                                0xA4, 0x74, 0x65, 0x73, 0x74, 0xA8, 0x50, 0x61, 0x73, 0x73, 0x77, 0x6F, 0x72, 0x64,
                                0xA5, 0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x83, 0xA4, 0x53, 0x69, 0x7A, 0x65, 0xCD, 0x07,
                                0xE7, 0xA4, 0x4E, 0x61, 0x6D, 0x65, 0xA8, 0x74, 0x65, 0x73, 0x74, 0x2E, 0x74, 0x78,
                                0x74, 0xA5, 0x42, 0x79, 0x74, 0x65, 0x73, 0xC4, 0x08, 0xF0, 0x9F, 0x90, 0xAE, 0xF0,
                                0x9F, 0x8D, 0xBA, 0xA7, 0x6F, 0x70, 0x74, 0x69, 0x6F, 0x6E, 0x73, 0x81, 0xA8, 0x63,
                                0x6F, 0x6D, 0x70, 0x72, 0x65, 0x73, 0x73, 0xC3, 0xA2, 0x69, 0x64, 0xCD, 0x23, 0x04,
                                0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73, 0x74
                            }
                        }
                    })!,
            };

    public static IEnumerable<object?[]> SerializePacketIdNamespaceDataCases => SerializePacketIdNamespaceDataTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.packetId,
            x.nsp,
            x.options,
            x.data,
            x.expected
        });

    [Theory]
    [MemberData(nameof(SerializePacketIdNamespaceDataCases))]
    public void Should_serialize_packet_id_and_namespace_and_data(
        int caseId,
        int packetId,
        string nsp,
        MessagePackSerializerOptions? options,
        object[] data,
        List<SerializedItem> expected)
    {
        var serializer = new SocketIOMessagePackSerializer(options);
        serializer.Serialize(packetId, nsp, data)
            .Should().BeEquivalentTo(expected);
    }
}