namespace Ch06Ex02Inheritance;

// Base class: every shape has a bounding Height and Width and an Area.
public abstract class Shape
{
    public double Height { get; set; }
    public double Width { get; set; }

    // Each derived shape must implement its own area formula.
    public abstract double Area { get; }
}

public class Rectangle : Shape
{
    public Rectangle(double height, double width)
    {
        Height = height;
        Width = width;
    }

    public override double Area => Height * Width;
}

public class Square : Shape
{
    public Square(double side)
    {
        Height = side;
        Width = side;
    }

    public override double Area => Height * Width;
}

public class Circle : Shape
{
    public double Radius { get; }

    public Circle(double radius)
    {
        Radius = radius;
        // The bounding box of a circle is its diameter in both directions.
        Height = Width = radius * 2;
    }

    public override double Area => Math.PI * Radius * Radius;
}
