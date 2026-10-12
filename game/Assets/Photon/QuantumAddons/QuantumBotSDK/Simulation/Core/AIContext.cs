namespace Quantum
{
  public partial struct AIContextUser
  {
  }

  public unsafe static class AIContextExtensions
  {
    public static ref AIContextUser UserData(this ref AIContext aiContext)
    {
      return ref *(AIContextUser*)aiContext.UserData;
    }
  }
}
