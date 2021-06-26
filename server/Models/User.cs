namespace Server.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Id { get; set; }

        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }

            return Username == (obj as User).Username;
        }

        public override int GetHashCode()
        {
            return Username.GetHashCode();
        }
    }
}