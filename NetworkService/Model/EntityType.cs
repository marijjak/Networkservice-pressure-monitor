namespace NetworkService.Model
{
    public class EntityType
    {

        public string Name { get; set; }


        public string ImagePath { get; set; }

        public override string ToString() => Name;
    }
}