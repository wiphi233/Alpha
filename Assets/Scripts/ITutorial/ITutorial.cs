public enum TutorialSet
{
    BeginTutorial
}

public interface ITutorial
{
    void EnterTutorial(TutorialSet TutorialID);
}