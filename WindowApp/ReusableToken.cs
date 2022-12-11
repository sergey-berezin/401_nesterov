using System.Threading;


namespace WindowApp
{
    public class ReusableToken
    {
        private CancellationTokenSource token_src;
        public CancellationToken token;
        
        public ReusableToken()
        {
            Reset();
        }

        public bool Cancelled()
        {
            return token.IsCancellationRequested;
        }

        public void Reset()
        {
            token_src = new CancellationTokenSource();
            token = token_src.Token;
        }

        public void Cancel()
        {
            token_src.Cancel();
        }
    }
}
