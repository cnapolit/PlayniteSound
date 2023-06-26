namespace PlayniteSounds.Services.Files
{
    public interface INormalizer
    {
        void CreateNormalizationDialogue();
        bool NormalizeAudioFile(string filePath);
    }
}