export const ApiUrl = import.meta.env.VITE_API_URL;

/**
 * The viewport width below which the app switches to its mobile layout, where the data tables
 * stack every record into a card. Keep in sync with `$mobile-breakpoint` in `src/styles/_variables.scss`.
 */
export const MobileBreakpoint = "960px";

/**
 * Tooltip placement for the action buttons of a data table row. Those buttons sit in the last column,
 * hard against the right edge of the viewport, and PrimeReact still opens a tooltip to the side there
 * rather than flipping it - which left the label to overflow the viewport once it stopped wrapping.
 * Above the button the room is always there, whatever the label says.
 */
export const RowActionTooltipOptions = { position: "top" } as const;
