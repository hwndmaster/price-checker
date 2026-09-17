import moment from "moment";
import { ticksToDate } from "@hwndmaster/atom-web-core";
import { TimeStamp } from "@/models/types";

/**
 * Formats a price in euro, e.g. `€ 1,234.56`.
 * @param price The price to format.
 * @returns The formatted price, or an empty string when the price is unknown.
 */
export function formatPrice(price: number | null | undefined): string {
    if (price == null) {
        return "";
    }

    return `€ ${price.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/**
 * Formats a timestamp as a humanized date, e.g. `2 days ago`.
 * @param ticks The timestamp.
 * @returns The humanized date, or an empty string when the timestamp is unknown.
 */
export function formatDateFromTicks(ticks: TimeStamp | null | undefined): string {
    if (ticks == null || ticks === 0) {
        return "";
    }

    return moment(ticksToDate(ticks)).fromNow();
}

/**
 * Formats a timestamp as an exact date and time.
 * @param ticks The timestamp.
 * @returns The formatted date, or an empty string when the timestamp is unknown.
 */
export function formatExactDateFromTicks(ticks: TimeStamp | null | undefined): string {
    if (ticks == null || ticks === 0) {
        return "";
    }

    return moment(ticksToDate(ticks)).format("YYYY-MM-DD HH:mm");
}
