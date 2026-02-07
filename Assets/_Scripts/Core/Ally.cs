namespace _Scripts.Core
{
    public class Ally : MovableEntitiy
    {
        protected override void PlayDeathSound()
        {
            SoundController.Instance?.PlayAllyDeathSound();
        }
    }
}
