namespace FlexSearch.Api.Types
{
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class IndexConfiguration
    {
        #region Fields

        private int commitTimeSec;

        private DirectoryType directoryType;

        private int ramBufferSizeMb;

        private int refreshTimeMilliSec;

        private int shards;

        public IndexConfiguration()
        {
            commitTimeSec = 60;
            directoryType = DirectoryType.FileSystem;
            ramBufferSizeMb = 500;
            refreshTimeMilliSec = 25;
            shards = 1;
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public int CommitTimeSec
        {
            get
            {
                return this.commitTimeSec;
            }

            set
            {
                this.commitTimeSec = value;
            }
        }

        [DataMember(Order = 2)]
        public DirectoryType DirectoryType
        {
            get
            {
                return this.directoryType;
            }

            set
            {
                this.directoryType = value;
            }
        }

        [DataMember(Order = 3)]
        public int RamBufferSizeMb
        {
            get
            {
                return this.ramBufferSizeMb;
            }

            set
            {
                this.ramBufferSizeMb = value;
            }
        }

        [DataMember(Order = 4)]
        public int RefreshTimeMilliSec
        {
            get
            {
                return this.refreshTimeMilliSec;
            }

            set
            {
                this.refreshTimeMilliSec = value;
            }
        }

        [DataMember(Order = 5)]
        public int Shards
        {
            get
            {
                return this.shards;
            }

            set
            {
                this.shards = value;
            }
        }

        #endregion
    }
}