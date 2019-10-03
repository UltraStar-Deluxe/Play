using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: What is Rap and RapGolden?
// The guide at https://thebrickyblog.wordpress.com/2011/01/27/ultrastar-txt-files-in-more-depth/
// only names Normal, Golden, and Freestyle notes.
public enum ENoteType
{
    // 1 Pointweight, pitch
    Normal,
    // 2 Pointweight, pitch
    Golden,
    // 0 Pointweight, no pitch
    Freestyle,
    // 1 Pointweight, no pitch
    Rap,
    // 2 Pointweight, no pitch
    RapGolden
}
