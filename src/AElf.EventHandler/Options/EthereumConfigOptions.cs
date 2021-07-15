namespace AElf.EventHandler
{
    public class EthereumConfigOptions
    {
        public string Url { get; set; }
        public string Address { get; set; }
        public string PrivateKey { get; set; }
        public bool IsEnable { get; set; }
        public string TransmitContractAddress { get; set; }
        public string LockMappingContractAddress { get; set; }
    }
}