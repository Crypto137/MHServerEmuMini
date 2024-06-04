using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.Core.VectorMath;
using MHServerEmuMini.Frontend;
using MHServerEmuMini.Games.Commands;
using MHServerEmuMini.Games.Entities;
using MHServerEmuMini.Games.GameData;
using MHServerEmuMini.Games.Regions;

namespace MHServerEmuMini.Games.Network
{
    public class PlayerConnection
    {
        private const ushort MuxChannel = 1;    // hardcoded to channel 1 for now

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly FrontendClient _frontendClient;
        private readonly List<IMessage> _pendingMessageList = new();

        public Player Player { get; }
        public WorldEntity Waypoint { get; }
        public Region Region { get; private set; }

        public bool IsLoading { get; private set; }

        public Game Game { get; }

        public PlayerConnection(Game game, FrontendClient frontendClient)
        {
            Game = game;
            _frontendClient = frontendClient;

            // Get region
            Region = Game.RegionManager.GetRegion(RegionPrototypeRef.AvengersTowerHUBRegion);

            // Do some basic entity management here

            // No need to set player prototype data ref, it's always the same
            Player = new(Game);
            Player.Id = Game.NextEntityId;
            Player.PlayerName.Value = ConfigManager.Instance.GetConfig<GameConfig>().PlayerName;
            Player.Initialize();

            // AvatarLibrary
            foreach (var avatarProtoRef in Enum.GetValues<AvatarPrototypeRef>())
                Player.CreateAvatar(avatarProtoRef);

            // Waypoint entity
            Waypoint = new(Game);
            Waypoint.Id = Game.NextEntityId;
            Waypoint.PrototypeDataRef = (ulong)WaypointPrototypeRef.AvengersTowerHUBwaypoint;
            Waypoint.Initialize();
        }

        #region NetClient Implementation

        // Do not use these methods directly, these are for the PlayerConnectionManager.
        // C# has no friends T_T

        /// <summary>
        /// Adds a new <see cref="IMessage"/> to the pending message list.
        /// </summary>
        /// <remarks>
        /// This should be called only by the <see cref="PlayerConnectionManager"/> this <see cref="PlayerConnection"/>
        /// belongs to, do not call this directly!
        /// </remarks>
        public void PostMessage(IMessage message)
        {
            _pendingMessageList.Add(message);
        }

        /// <summary>
        /// Sends all pending <see cref="IMessage"/> instances.
        /// </summary>
        /// <remarks>
        /// This should be called only by the <see cref="PlayerConnectionManager"/> this <see cref="PlayerConnection"/>
        /// belongs to, do not call this directly!
        /// </remarks>
        public void FlushMessages()
        {
            if (_pendingMessageList.Any() == false) return;
            _frontendClient.SendMessages(MuxChannel, _pendingMessageList);
            _pendingMessageList.Clear();
        }

        public bool CanSendOrReceiveMessages()
        {
            // TODO: Block message processing during certain states (e.g. malicious client sending messages while loading).
            return true;
        }

        public void OnDisconnect()
        {
            // Post-disconnection cleanup (save data, remove entities, etc).

        }

        #endregion

        public void EnterGame()
        {
            Logger.Trace("Entering game");
            IsLoading = true;

            SendMessage(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime((ulong)Clock.GameTime.TotalMilliseconds)
                .SetCurrentservergameid(0x1000000000000001)
                .Build());

            SendMessage(NetMessageServerVersion.CreateBuilder().SetVersion(Game.Version).Build());

            SendMessage(NetMessageLocalPlayer.CreateBuilder().SetLocalPlayerEntityId(Player.Id).Build());

            // NOTE: Archive version >= 30

            // Create player entity
            SendMessage(NetMessageEntityCreate.CreateBuilder()
                .SetIdEntity(Player.Id)
                .SetPrototypeId(Player.PrototypeDataRef)
                .SetDbId(0x2000000000000001)
                .SetInterestPolicies((uint)AOINetworkPolicyValues.AOIChannelOwner)
                .SetArchiveData(Player.ToByteString())
                .Build());

            if (Player.CurrentAvatar == null)
            {
                // Check if there is a default avatar specified in config
                if (Enum.TryParse(ConfigManager.Instance.GetConfig<GameConfig>().DefaultAvatar, out AvatarPrototypeRef avatarProtoRef))
                {
                    ulong entityId = Player.GetAvatarEntityId(avatarProtoRef);
                    SwitchAvatar(entityId);
                }
                else
                {
                    // Ask the player to select an avatar if there is no current avatar
                    SendMessage(NetMessageSelectStartingAvatarForNewPlayer.DefaultInstance);
                    return;
                }
            }

            // Create avatar in play entity
            SendMessage(NetMessageEntityCreate.CreateBuilder()
                .SetIdEntity(Player.CurrentAvatar.Id)
                .SetPrototypeId(Player.CurrentAvatar.PrototypeDataRef)
                .SetDbId(0)
                .SetInterestPolicies((uint)AOINetworkPolicyValues.AOIChannelOwner)
                .SetInvLocContainerEntityId(Player.Id)
                .SetInvLocInventoryPrototypeId((ulong)InventoryPrototypeRef.PlayerAvatarInPlay)
                .SetInvLocSlot(0)
                .SetArchiveData(Player.CurrentAvatar.ToByteString())
                .Build());

            // Create avatar library entities
            for (int i = 0; i < Player.AvatarLibrary.Count; i++)
            {
                Avatar avatar = Player.AvatarLibrary[i];

                SendMessage(NetMessageEntityCreate.CreateBuilder()
                    .SetIdEntity(avatar.Id)
                    .SetPrototypeId(avatar.PrototypeDataRef)
                    .SetDbId(0)
                    .SetInterestPolicies((uint)AOINetworkPolicyValues.AOIChannelOwner)
                    .SetInvLocContainerEntityId(Player.Id)
                    .SetInvLocInventoryPrototypeId((ulong)InventoryPrototypeRef.PlayerAvatarLibrary)
                    .SetInvLocSlot((uint)i)
                    .SetArchiveData(avatar.ToByteString())
                    .Build());
            }

            SendMessage(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build());

            SendMessage(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId(Region.PrototypeDataRef)
                .Build());
            
            // Create region
            SendMessage(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(1)
                .SetServerGameId(Game.Id)
                .SetClearingAllInterest(false)
                .SetRegionPrototypeId(Region.PrototypeDataRef)
                .SetRegionRandomSeed(1)
                .SetRegionMin(Region.Min.ToNetStructPoint3())
                .SetRegionMax(Region.Max.ToNetStructPoint3())
                .SetCreateRegionParams(NetStructCreateRegionParams.CreateBuilder().SetLevel(1).SetDifficultyProtoRef(0))
                .Build());

            foreach (var areaKvp in Region.Areas)
            {
                SendMessage(NetMessageAddArea.CreateBuilder()
                    .SetAreaId(areaKvp.Key)
                    .SetAreaPrototypeId(areaKvp.Value.PrototypeDataRef)
                    .SetAreaOrigin(areaKvp.Value.Origin.ToNetStructPoint3())
                    .Build());

                foreach (var cellKvp in areaKvp.Value.Cells)
                {
                    SendMessage(NetMessageCellCreate.CreateBuilder()
                        .SetAreaId(areaKvp.Key)
                        .SetCellId(cellKvp.Key)
                        .SetCellPrototypeId(cellKvp.Value.PrototypeDataRef)
                        .SetPositionInArea(cellKvp.Value.Position.ToNetStructPoint3())
                        .SetCellRandomSeed(1)
                        .SetBufferwidth(0)
                        .SetOverrideLocationName(0)
                        .SetDepth(0)
                        .Build());
                }
            }

            SendMessage(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build());
        }

        public void EnterGameWorld()
        {
            Logger.Trace("Entering game world");

            // Create waypoint entity
            SendMessage(NetMessageEntityCreate.CreateBuilder()
                .SetIdEntity(Waypoint.Id)
                .SetPrototypeId(Waypoint.PrototypeDataRef)
                .SetDbId(0)
                .SetInterestPolicies((uint)AOINetworkPolicyValues.AOIChannelDiscovery)
                .SetPosition(Region.EntrancePosition.ToNetStructPoint3())
                .SetOrientation(Orientation.Zero.ToNetStructPoint3())
                .SetArchiveData(ByteString.Empty)
                .Build());

            Player.CurrentAvatar.Position = Region.EntrancePosition;
            Player.CurrentAvatar.Orientation = Region.EntranceOrientation;

            SendMessage(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetEntityId(Player.CurrentAvatar.Id)
                .SetPosition(Player.CurrentAvatar.Position.ToNetStructPoint3())
                .SetOrientation(Player.CurrentAvatar.Orientation.ToNetStructPoint3())
                .SetLocomotionState(NetStructLocomotionState.CreateBuilder()
                    .SetMovespeed(350f)
                    .SetUpdatepathnodes(false))
                .Build());

            AssignPower(Player.CurrentAvatar.Id, 9479769786738480443);  // BodyslideToTown

            SendMessage(NetMessageDequeueLoadingScreen.DefaultInstance);
            IsLoading = false;
        }

        public void ExitGame()
        {
            Logger.Trace("Exiting game");
            SendMessage(NetMessageBeginExitGame.DefaultInstance);
            SendMessage(NetMessageRegionChange.CreateBuilder().SetRegionId(0).SetServerGameId(0).SetClearingAllInterest(true).Build());
            SendMessage(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId(Region.PrototypeDataRef).Build());
        }

        public bool SwitchAvatar(ulong entityId)
        {
            if (entityId == 0) return Logger.WarnReturn(false, $"SwitchAvatar(): entityId == 0");

            Player.SwitchAvatar(entityId, out Avatar prevAvatar, out int prevAvatarSlot);
            if (prevAvatar == null) return true;    // This is a starting avatar selection switch, no need to update the client

            // Destroy the avatar we are switching to on the client
            SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(Player.CurrentAvatar.Id).Build());

            // Destroy the previous avatar on the client
            SendMessage(NetMessageChangeAOIPolicies.CreateBuilder()
                .SetIdEntity(prevAvatar.Id)
                .SetCurrentpolicies((uint)AOINetworkPolicyValues.AOIChannelOwner)
                .SetExitGameWorld(true)
                .Build());

            SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(prevAvatar.Id).Build());

            // Recreate previous avatar
            SendMessage(NetMessageEntityCreate.CreateBuilder()
                .SetIdEntity(prevAvatar.Id)
                .SetPrototypeId(prevAvatar.PrototypeDataRef)
                .SetDbId(0)
                .SetInterestPolicies((uint)AOINetworkPolicyValues.AOIChannelOwner)
                .SetInvLocContainerEntityId(Player.Id)
                .SetInvLocInventoryPrototypeId((ulong)InventoryPrototypeRef.PlayerAvatarLibrary)
                .SetInvLocSlot((uint)prevAvatarSlot)
                .SetArchiveData(prevAvatar.ToByteString())
                .Build());

            // Recreate the avatar we just switched to
            SendMessage(NetMessageEntityCreate.CreateBuilder()
                .SetIdEntity(Player.CurrentAvatar.Id)
                .SetPrototypeId(Player.CurrentAvatar.PrototypeDataRef)
                .SetDbId(0)
                .SetInterestPolicies((uint)AOINetworkPolicyValues.AOIChannelOwner)
                .SetPosition(Player.CurrentAvatar.Position.ToNetStructPoint3())
                .SetOrientation(Player.CurrentAvatar.Orientation.ToNetStructPoint3())
                .SetLocomotionState(NetStructLocomotionState.CreateBuilder()
                    .SetMovespeed(350f)
                    .SetUpdatepathnodes(false))
                .SetInvLocContainerEntityId(Player.Id)
                .SetInvLocInventoryPrototypeId((ulong)InventoryPrototypeRef.PlayerAvatarInPlay)
                .SetInvLocSlot(0)
                .SetArchiveData(Player.CurrentAvatar.ToByteString())
                .Build());

            AssignPower(Player.CurrentAvatar.Id, 9479769786738480443);  // BodyslideToTown

            return true;
        }

        public void AssignPower(ulong entityId, ulong powerProtoRef)
        {
            SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(entityId)
                .SetPowerProtoId(powerProtoRef)
                .SetTargetentityid(entityId)
                .SetPowerRank(1)
                .SetCharacterLevel(1)
                .SetItemLevel(1)
                .SetPowerCollectionIsduplicating(false)
                .Build());
        }

        public void MoveToRegion(RegionPrototypeRef regionProtoRef)
        {
            Region = Game.RegionManager.GetRegion(regionProtoRef);
            ExitGame();
            Game.NetworkManager.SetPlayerConnectionPending(this);
        }

        public void SendSystemChatMessage(string message)
        {
            _frontendClient.SendMessage(2, ChatNormalMessage.CreateBuilder()
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                .SetFromPlayerName(string.Empty)
                .SetToPlayerName(string.Empty)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(message))
                .Build());
        }

        public void PlayKismetSeq(ulong kismetSeqProtoRef)
        {
            SendMessage(NetMessagePlayKismetSeq.CreateBuilder().SetKismetSeqPrototypeId(kismetSeqProtoRef).Build());
        }

        #region Message Handling

        /// <summary>
        /// Sends an <see cref="IMessage"/> instance over this <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(IMessage message)
        {
            Game.SendMessage(this, message);
        }

        /// <summary>
        /// Handles a <see cref="MailboxMessage"/>.
        /// </summary>
        public void ReceiveMessage(MailboxMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:     OnUpdateAvatarState(message); break;
                case ClientToGameServerMessage.NetMessageCellLoaded:            OnCellLoaded(message); break;
                case ClientToGameServerMessage.NetMessageAdminCommand:          OnAdminCommand(message); break;
                case ClientToGameServerMessage.NetMessagePing:                  OnPing(message); break;
                case ClientToGameServerMessage.NetMessageUseWaypoint:           OnUseWaypoint(message); break;
                case ClientToGameServerMessage.NetMessageSwitchAvatar:          OnSwitchAvatar(message); break;
                case ClientToGameServerMessage.NetMessageReturnToHub:           OnReturnToHub(message); break;
                case ClientToGameServerMessage.NetMessageChat:                  OnChat(message); break;
                case ClientToGameServerMessage.NetMessageCreateNewPlayerWithSelectedStartingAvatar:    OnCreateNewPlayerWithSelectedStartingAvatar(message); break;
                case ClientToGameServerMessage.NetMessageGracefulDisconnect:    OnGracefulDisconnect(message); break;
                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool OnUpdateAvatarState(MailboxMessage message)
        {
            var updateAvatarState = message.As<NetMessageUpdateAvatarState>();
            if (updateAvatarState == null) return Logger.WarnReturn(false, $"OnUpdateAvatarState(): Failed to retrieve message");

            Player.CurrentAvatar.Position = new(updateAvatarState.Position);
            Player.CurrentAvatar.Orientation = new(updateAvatarState.Orientation.X, 0f, 0f);

            return true;
        }

        private bool OnCellLoaded(MailboxMessage message)
        {
            var cellLoaded = message.As<NetMessageCellLoaded>();
            if (cellLoaded == null) return Logger.WarnReturn(false, $"OnCellLoaded(): Failed to retrieve message");

            Logger.Trace($"NetMessageCellLoaded: regionId={cellLoaded.RegionId}, cellId={cellLoaded.CellId}");
            if (IsLoading)
                EnterGameWorld();
            return true;
        }

        private bool OnAdminCommand(MailboxMessage message)
        {
            var adminCommand = message.As<NetMessageAdminCommand>();
            if (adminCommand == null) return Logger.WarnReturn(false, $"OnAdminCommand(): Failed to retrieve message");

            Logger.Debug($"NetMessageAdminCommand: {adminCommand.Command}");

            return true;
        }

        private bool OnPing(MailboxMessage message)
        {
            var ping = message.As<NetMessagePing>();
            if (ping == null) return Logger.WarnReturn(false, $"OnPing(): Failed to retrieve message");

            //Logger.Debug($"NetMessagePing");

            return true;
        }

        private bool OnUseWaypoint(MailboxMessage message)
        {
            var useWaypoint = message.As<NetMessageUseWaypoint>();
            if (useWaypoint == null) return Logger.WarnReturn(false, $"OnUseWaypoint(): Failed to retrieve message");

            Logger.Trace($"NetMessageUseWaypoint\t{useWaypoint}");
            RegionPrototypeRef regionProtoRef = (WaypointTargetPrototypeRef)useWaypoint.WaypointDataRef switch
            {
                WaypointTargetPrototypeRef.AvengersTowerHub         => RegionPrototypeRef.AvengersTowerHUBRegion,
                WaypointTargetPrototypeRef.XMansionBlackbird        => RegionPrototypeRef.XaviersMansionRegion,
                WaypointTargetPrototypeRef.HelicarrierHub           => RegionPrototypeRef.HelicarrierRegion,
                WaypointTargetPrototypeRef.TrainingRoomSHIELD       => RegionPrototypeRef.TrainingRoomSHIELDRegion,
                WaypointTargetPrototypeRef.RaftLandingPad           => RegionPrototypeRef.RaftRegion,
                WaypointTargetPrototypeRef.BrownstoneRoofs          => RegionPrototypeRef.HellsKitchen01RegionA,
                WaypointTargetPrototypeRef.BrownstoneSubwayEntrance => RegionPrototypeRef.SubwayHK01Region,
                WaypointTargetPrototypeRef.NightClubEntrance        => RegionPrototypeRef.NightclubRegion,
                WaypointTargetPrototypeRef.TrainingForestArea       => RegionPrototypeRef.ClassifiedBovineSectorRegion,
                _ => 0,
            };

            if (regionProtoRef == 0)
            {
                SendSystemChatMessage($"Waypoint destination {useWaypoint.WaypointDataRef} is not available.");
                return false;
            }

            MoveToRegion(regionProtoRef);
            return true;
        }

        private bool OnSwitchAvatar(MailboxMessage message)
        {
            var switchAvatar = message.As<NetMessageSwitchAvatar>();
            if (switchAvatar == null) return Logger.WarnReturn(false, $"OnSwitchAvatar(): Failed to retrieve message");

            SwitchAvatar(switchAvatar.AvatarId);
            return true;
        }

        private bool OnReturnToHub(MailboxMessage message)
        {
            var returnToHub = message.As<NetMessageReturnToHub>();
            if (returnToHub == null) return Logger.WarnReturn(false, $"OnReturnToHub(): Failed to retrieve message");

            MoveToRegion(RegionPrototypeRef.AvengersTowerHUBRegion);
            return true;
        }

        private bool OnChat(MailboxMessage message)
        {
            var chat = message.As<NetMessageChat>();
            if (chat == null) return Logger.WarnReturn(false, $"OnChat(): Failed to retrieve message");

            if (CommandManagerMini.Instance.TryParse(chat.TheMessage.Body, this))
                return true;

            Logger.Trace($"[{chat.RoomType}] {chat.TheMessage.Body}");
            _frontendClient.SendMessage(2, ChatNormalMessage.CreateBuilder()
                .SetRoomType(chat.RoomType)
                .SetFromPlayerName(Player.PlayerName.Value)
                .SetToPlayerName(string.Empty)
                .SetTheMessage(chat.TheMessage)
                .Build());

            return true;
        }

        public bool OnCreateNewPlayerWithSelectedStartingAvatar(MailboxMessage message)
        {
            var createNewPlayer = message.As<NetMessageCreateNewPlayerWithSelectedStartingAvatar>();
            if (createNewPlayer == null) return Logger.WarnReturn(false, $"OnCreateNewPlayerWithSelectedStartingAvatar(): Failed to retrieve message");

            ulong entityId = Player.GetAvatarEntityId((AvatarPrototypeRef)createNewPlayer.StartingAvatarPrototypeId);
            SwitchAvatar(entityId);
            ExitGame();
            Game.NetworkManager.SetPlayerConnectionPending(this);

            return true;
        }

        public bool OnGracefulDisconnect(MailboxMessage message)
        {
            SendMessage(NetMessageGracefulDisconnectAck.DefaultInstance);
            return true;
        }

        #endregion
    }
}
