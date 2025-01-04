using System.Threading.Tasks;

namespace PlayniteSounds.Services.Files
{
    public interface INormalizer
    {
        void       CreateNormalizationDialogue();
        bool       NormalizeAudioFile(string filePath);
        Task<bool> NormalizeAudioFileAsync(string filePath);
    }
}