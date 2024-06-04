using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Contains a serialized <see cref="IMessage"/>.
    /// </summary>
    public class MessagePackage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Type Protocol { get; set; }
        public uint Id { get; }
        public byte[] Payload { get; }
        public TimeSpan GameTimeReceived { get; set; }
        public TimeSpan DateTimeReceived { get; set; }

        /// <summary>
        /// Constructs a new <see cref="MessagePackage"/> from raw data.
        /// </summary>
        public MessagePackage(uint id, byte[] payload)
        {
            Id = id;
            Payload = payload;
        }

        /// <summary>
        /// Constructs a new <see cref="MessagePackage"/> from an <see cref="IMessage"/>.
        /// </summary>
        public MessagePackage(IMessage message)
        {
            (Protocol, Id) = ProtocolDispatchTable.Instance.GetMessageProtocolId(message);
            Payload = message.ToByteArray();
        }

        /// <summary>
        /// Decodes a <see cref="MessagePackage"/> from the provided <see cref="CodedInputStream"/>.
        /// </summary>
        public MessagePackage(CodedInputStream stream)
        {
            try
            {
                Id = stream.ReadRawVarint32();
                Payload = stream.ReadRawBytes((int)stream.ReadRawVarint32());
            }
            catch (Exception e)
            {
                Id = 0;
                Payload = null;
                Logger.ErrorException(e, "GameMessage construction failed");
            }
        }

        /// <summary>
        /// Encodes the <see cref="MessagePackage"/> to the provided <see cref="CodedOutputStream"/>.
        /// </summary>
        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32(Id);

            if (Protocol == typeof(GameServerToClientMessage) && NoLatencyBufferMessages.Contains(Id) == false)
            {
                using (MemoryStream ms = new())
                {
                    CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                    cos.WriteRawVarint64(CodedOutputStream.EncodeZigZag64(Clock.GameTime.Ticks / 10));
                    cos.WriteRawBytes(Payload);
                    cos.Flush();

                    byte[] buffer = ms.ToArray();

                    stream.WriteRawVarint32((uint)buffer.Length);
                    stream.WriteRawBytes(buffer);
                }
            }
            else
            {
                stream.WriteRawVarint32((uint)Payload.Length);
                stream.WriteRawBytes(Payload);
            }
        }

        /// <summary>
        /// Serializes the <see cref="MessagePackage"/> instance to a byte array.
        /// </summary>
        public byte[] Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                Encode(cos);
                cos.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the payload as an <see cref="IMessage"/> using the assigned protocol. Returns <see langword="null"/> if deserialization failed.
        /// </summary>
        public IMessage Deserialize()
        {
            if (Protocol == null) return Logger.WarnReturn<IMessage>(null, $"Deserialize(): Protocol == null");

            try
            {
                var parse = ProtocolDispatchTable.Instance.GetParseMessageDelegate(Protocol, Id);
                return parse(Payload);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"{nameof(Deserialize)}");
                return null;
            }
        }

        // Messages that contain option 50001 in the descriptor should not be timestamped
        // TODO: Confirm if all of these are working correctly
        private static readonly HashSet<uint> NoLatencyBufferMessages = [
            (uint)GameServerToClientMessage.NetMessageReadyAndLoggedIn,
            (uint)GameServerToClientMessage.NetMessageReadyForTimeSync,
            (uint)GameServerToClientMessage.NetMessageAdminCommandResponse,
            (uint)GameServerToClientMessage.NetMessageContinuousPowerUpdateToClient,
            (uint)GameServerToClientMessage.NetMessageRegionPrimitiveBox,
            (uint)GameServerToClientMessage.NetMessageRegionPrimitiveTriangle,
            (uint)GameServerToClientMessage.NetMessageRegionPrimitiveSphere,
            (uint)GameServerToClientMessage.NetMessageRegionPrimitiveLine,
            (uint)GameServerToClientMessage.NetMessageSystemMessage,
            (uint)GameServerToClientMessage.NetMessageMissionDebugUIUpdate,
            (uint)GameServerToClientMessage.NetMessageDebugEntityPosition,
            (uint)GameServerToClientMessage.NetMessageServerVersion,
            (uint)GameServerToClientMessage.NetStructPrefetchEntityPower,
            (uint)GameServerToClientMessage.NetStructPrefetchCell,
            (uint)GameServerToClientMessage.NetMessagePrefetchRegionsForDownload,
            (uint)GameServerToClientMessage.NetMessageQueryIsRegionAvailable,
            (uint)GameServerToClientMessage.NetStructMatchQueueEntry,
            (uint)GameServerToClientMessage.NetMessageMatchQueueListResponse,
            (uint)GameServerToClientMessage.NetMessageMatchQueueResponse,
            (uint)GameServerToClientMessage.NetMessageMatchInviteNotification,
            (uint)GameServerToClientMessage.NetMessageMatchStatsResponse,
            (uint)GameServerToClientMessage.NetMessageChatFromGameSystem,
            (uint)GameServerToClientMessage.NetMessageChatError,
            (uint)GameServerToClientMessage.NetMessageCatalogItems,
            (uint)GameServerToClientMessage.NetMessageGetCurrencyBalanceResponse,
            (uint)GameServerToClientMessage.NetMessageBuyItemFromCatalogResponse,
            (uint)GameServerToClientMessage.NetMessageServerNotification,
            (uint)GameServerToClientMessage.NetMessageSyncTimeReply,
            (uint)GameServerToClientMessage.NetMessageForceDisconnect,
            (uint)GameServerToClientMessage.NetMessageCraftingFinished,
            (uint)GameServerToClientMessage.NetMessageCraftingFailed,
            (uint)GameServerToClientMessage.MessageReportEntry,
            (uint)GameServerToClientMessage.NetMessageMessageReport,
            (uint)GameServerToClientMessage.NetMessageConsoleMessage,
            (uint)GameServerToClientMessage.NetMessageReloadPackagesStart,
            (uint)GameServerToClientMessage.NetMessagePlayStoryBanter,
            (uint)GameServerToClientMessage.NetMessagePlayKismetSeq,
            (uint)GameServerToClientMessage.NetMessageGracefulDisconnectAck,
            (uint)GameServerToClientMessage.NetMessageLiveTuningUpdate,
            (uint)GameServerToClientMessage.NetMessageUpdateSituationalTarget,
            (uint)GameServerToClientMessage.NetMessageItemBindingChanged,
            (uint)GameServerToClientMessage.NetMessageItemsHeldForRecovery,
            (uint)GameServerToClientMessage.NetMessageItemRecovered,
            (uint)GameServerToClientMessage.NetMessageSwitchToPendingNewAvatarFailed,
            (uint)GameServerToClientMessage.NetMessageMetaGameWaveUpdate,
            (uint)GameServerToClientMessage.NetMessagePvEInstanceCrystalUpdate,
            (uint)GameServerToClientMessage.NetMessagePvEInstanceDeathUpdate,
            (uint)GameServerToClientMessage.NetMessageShowTutorialTip
        ];
    }
}
