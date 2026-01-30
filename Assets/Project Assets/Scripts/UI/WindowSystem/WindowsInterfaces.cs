using System.Threading.Tasks;
using UnityEngine;

public interface IWindowAnimation
{
    Task AnimateOpen(GameObject windowContent);
    Task AnimateClose(GameObject windowContent);
}