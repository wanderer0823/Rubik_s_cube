using System.Collections.Generic;  
using UnityEngine;  
using UnityEngine.UI;  


public class ArrowsButton : MonoBehaviour
{
    // 箭头按钮：根据当前朝向面和箭头位置，筛选出对应一层的立方体。  
    // 例如面向 Up 时，最左侧朝上的箭头对应左边竖列（x=-1）。  
    #region 箭头参数  
    public enum ArrowSide { Left, Up }  
  
    // 在边上的索引：0=左/上，1=中，2=右/下   
    [Range(0, 2)]  
    [SerializeField] private int arrowIndex = 0;  
  
    [SerializeField] private ArrowSide arrowSide;  
    #endregion
    
    void Awake()  
    {        
        Button button = GetComponent<Button>();  
    }  
    public void SetArrowSide(ArrowSide side)  
    { 
        arrowSide = side;  
    }  
    public void SetArrowIndex(int index)  
    {            
        arrowIndex = index;  
    }        
}
