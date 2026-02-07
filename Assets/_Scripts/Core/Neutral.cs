namespace _Scripts.Core
{
    public class Neutral : MovableEntitiy
    {
        protected override void PlayDeathSound()
        {
            SoundController.Instance?.PlayNeutralDeathSound();
        }
    }
}
