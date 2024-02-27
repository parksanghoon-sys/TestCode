public class NonSerializableType
{
	private int _id;

	public int Id
	{
		get { return _id; }
		set { _id = value; }
	}
	private DateTime _createTime;

	public DateTime CreateTime
	{
		get { return _createTime; }
		set { _createTime = value; }
	}
	public string[] Test { get; set; }

	public NonSerializableType(int id)
    {
        this._id = id;
		_createTime = DateTime.Now;
		Test = new string[] { "1", "2", "3" };
    }
}