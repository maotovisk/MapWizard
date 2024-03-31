namespace Beatmap
{
    /// <summary>
    /// Represents the events section of a beatmap.
    /// </summary>
    public class Events : IEvents
    {
        /// <summary>
        /// Represents the list of events in the beatmap.
        /// </summary>
        public List<IEvent> EventList { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Events"/> class.
        /// </summary>
        public Events()
        {
            EventList = new List<IEvent>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Events"/> class with the specified parameters.
        /// </summary>
        /// <param name="eventList"></param>
        public Events(List<IEvent> eventList)
        {
            EventList = eventList;
        }
    }
}