using System;

namespace appbox.Drawing.Printing
{
	[Serializable]
	public enum PaperSourceKind {
		AutomaticFeed = 7,
		Cassette = 14,
		Custom = 257,
		Envelope = 5,
		FormSource = 15,
		LargeCapacity = 11,
		LargeFormat = 10,
		Lower = 2,
		Manual = 4,
		ManualFeed = 6,
		Middle = 3,
		SmallFormat = 9,
		TractorFeed = 8,
		Upper = 1
	}
}
