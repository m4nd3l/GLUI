using Silk.NET.Input;

namespace GLUI.input;

public class KeyIndexMap {
	private const int minimumValue = -1;
	private const int maximumValue = 348;
	private const int tableLength = maximumValue - minimumValue + 1;

	private static readonly int[] lookupTable = buildLookupTable();

	public const int totalUniqueKeys = 121;

	private static int[] buildLookupTable() {
		int[] table = new int[tableLength];
		Array.Fill(table, -1);

		int nextIndex = 0;
		foreach (Key key in Enum.GetValues<Key>()) {
			int rawValue = (int)key;
			if (rawValue < minimumValue || rawValue > maximumValue)
				continue;

			int tablePosition = rawValue - minimumValue;
			if (table[tablePosition] == -1)
				table[tablePosition] = nextIndex++;
		}

		return table;
	}

	public static int getIndex(Key key) {
		int rawValue = (int)key;
		if (rawValue < minimumValue || rawValue > maximumValue)
			return -1;
		return lookupTable[rawValue - minimumValue];
	}
}

public static class MouseButtonIndexMap {
	private static readonly Dictionary<MouseButton, int> buttonToIndex = buildIndexMap();
	public static readonly int totalUniqueButtons = buttonToIndex.Count;

	private static Dictionary<MouseButton, int> buildIndexMap() {
		Dictionary<MouseButton, int> map = new Dictionary<MouseButton, int>();
		int nextIndex = 0;

		foreach (MouseButton button in Enum.GetValues<MouseButton>()) {
			if (!map.ContainsKey(button))
				map[button] = nextIndex++;
		}

		return map;
	}

	public static int getIndex(MouseButton button) {
		return buttonToIndex.TryGetValue(button, out int index) ? index : -1;
	}
}