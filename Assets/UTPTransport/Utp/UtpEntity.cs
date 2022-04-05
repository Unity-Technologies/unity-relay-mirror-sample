using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace Utp
{
    /// <summary>
    /// A server or client inside Utp.
    /// </summary>
    public abstract class UtpEntity
    {
        /// <summary>
        /// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
        /// </summary>
        protected NativeQueue<UtpConnectionEvent> connectionsEventsQueue;

        /// <summary>
        /// Used alongside a connection to connect, send, and receive data from a listen server.
        /// </summary>
        protected NetworkDriver driver;

        /// <summary>
		/// A pipeline on the driver that is sequenced, and ensures messages are delivered.
		/// </summary>
		protected NetworkPipeline reliablePipeline;

        /// <summary>
        /// A pipeline on the driver that is sequenced, but does not ensure messages are delivered.
        /// </summary>
        protected NetworkPipeline unreliablePipeline;

        /// <summary>
		/// Job handle to schedule jobs.
		/// </summary>
		protected JobHandle jobHandle;

        /// <summary>
        /// Timeout(ms) to be set on drivers.
        /// </summary>
        protected int timeout;

        /// <summary>
        /// Returns whether this Utp entity is active or not.
        /// </summary>
        /// <returns>The activity state of the entity.</returns>
        public bool IsActive()
        {
            return DriverIsActive(driver);
        }

        /// <summary>
        /// Returns whether a connection is a valid one. Checks against default connection object.
        /// </summary>
        /// <param name="connection">The connection to validate.</param>
        /// <returns>True or false, whether the connection is valid.</returns>
        public bool ConnectionIsActive(Unity.Networking.Transport.NetworkConnection connection)
        {
            return connection != default(Unity.Networking.Transport.NetworkConnection);
        }

        /// <summary>
		/// Determine whether the server is running or not.
		/// </summary>
		/// <returns>True if running, false otherwise.</returns>
		public bool DriverIsActive(NetworkDriver driver)
        {
            return !Equals(driver, default(NetworkDriver));
        }
    }
}