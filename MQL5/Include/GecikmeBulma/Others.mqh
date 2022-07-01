bool commentSep(string comment, string &result[], string sep="#")
{
  ushort u_sep;     // The code of the separator character
  // string result[];  // An array to get strings

  u_sep = StringGetCharacter(sep, 0);
  int k = StringSplit(comment, u_sep, result);
  if (k > 0)
  {
    return true;
  }

  return false;
}