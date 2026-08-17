/** Maps colour name strings to their hex values for display in the colour selector. */
export const COLOUR_HEX_MAP: Record<string, string> = {
  Black:   '#212121',
  White:   '#FFFFFF',
  Navy:    '#1A1A6B',
  Red:     '#E4002B',
  Pink:    '#FFC0CB',
  Blue:    '#0000FF',
  Grey:    '#808080',
  Green:   '#008000',
  Yellow:  '#FFFF00',
  Brown:   '#A52A2A',
  Silver:  '#C0C0C0',
  Regular: '#E0E0E0',
};

/** Returns the hex code for a colour name, falling back to a neutral grey. */
export function getColourHex(colour: string): string {
  return COLOUR_HEX_MAP[colour] ?? '#E0E0E0';
}
