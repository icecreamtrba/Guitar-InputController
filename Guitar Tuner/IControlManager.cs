namespace Guitar_Tuner
{
    public interface IControlManager
    {
        void HandleNote(string note);
        bool IsEnabled { get; set; }
    }
}