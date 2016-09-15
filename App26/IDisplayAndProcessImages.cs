namespace App26
{
  using System;
  using System.Threading.Tasks;

  // this is just a cheap way of passing one object to another and reducing the
  // surface area exposed as a depdendency. I'd look to IoC to do it right.
  public interface IDisplayAndProcessImages
  {
    Task DisplayImageAsync(Uri uri);
    Task ProcessAsync(Uri uri);
  }
}
