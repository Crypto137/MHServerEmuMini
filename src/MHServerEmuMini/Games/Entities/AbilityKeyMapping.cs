using MHServerEmu.Core.Serialization;

namespace MHServerEmuMini.Games.Entities
{
    public enum AbilitySlot
    {
        PrimaryAction = 0,          // Left Click
        SecondaryAction = 1,        // Right Click
        ActionKey0 = 2,             // A
        ActionKey1 = 3,             // S
        ActionKey2 = 4,             // D
        ActionKey3 = 5,             // F
        ActionKey4 = 6,             // G
        ActionKey5 = 7,             // H
    }

    public class AbilityKeyMapping : ISerialize
    {
        private ulong _associatedTransformMode;
        private ulong _primaryAction;
        private ulong _secondaryAction;
        private readonly ulong[] _actionKeys = new ulong[6];
        private readonly ulong[] _hotkeyData = [];     // actually not a ulong

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= archive.Transfer(ref _associatedTransformMode);
            success &= archive.Transfer(ref _primaryAction);
            success &= archive.Transfer(ref _secondaryAction);

            ulong numActionKeys = (ulong)_actionKeys.Length;
            success &= archive.Transfer(ref numActionKeys);
            for (ulong i = 0; i < numActionKeys; i++)
                success &= archive.Transfer(ref _actionKeys[i]);

            ulong numHotkeyData = (ulong)_hotkeyData.Length;
            success &= archive.Transfer(ref numHotkeyData);
            // TODO: hotkey data serialization

            return success;
        }

        public bool SetAbilityInAbilitySlot(ulong abilityProtoRef, AbilitySlot abilitySlot)
        {
            switch (abilitySlot)
            {
                case AbilitySlot.PrimaryAction:     _primaryAction = abilityProtoRef; break;
                case AbilitySlot.SecondaryAction:   _secondaryAction = abilityProtoRef; break;
                default:                            _actionKeys[(int)abilitySlot - 2] = abilityProtoRef; break;
            }

            return true;
        }
    }
}
