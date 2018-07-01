namespace ServerCopierSelf.Models
{
    /// <summary>
    /// The config model.
    /// </summary>
    public class ConfigModel
    {
        /// <summary>
        /// Gets or sets the bot prefix
        /// </summary>
        public string Prefix { get; set; } = "+";

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        public string Token { get; set; } = "Token";
    }
}