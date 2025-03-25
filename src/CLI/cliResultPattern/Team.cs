public record Team(string Name, List<Person> Members)
{
    public override string ToString()
    {
        return $"팀: {Name}, 인원: {Members.Count}명";
    }
}
