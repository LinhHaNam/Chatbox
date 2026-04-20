const VIETNAM_TIMEZONE = 'Asia/Ho_Chi_Minh';
const VIETNAMESE_LOCALE = 'vi-VN';

const parseDate = (value) => {
  if (!value) return null;

  let normalizedValue = value;
  if (
    typeof value === 'string' &&
    /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?$/.test(value)
  ) {
    normalizedValue = `${value}Z`;
  }

  const date = new Date(normalizedValue);
  return Number.isNaN(date.getTime()) ? null : date;
};

const getDateKey = (value) => {
  const date = parseDate(value);
  if (!date) return '';

  return new Intl.DateTimeFormat(VIETNAMESE_LOCALE, {
    timeZone: VIETNAM_TIMEZONE,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(date);
};

export const formatVietnamTime = (value) => {
  const date = parseDate(value);
  if (!date) return '';

  return new Intl.DateTimeFormat(VIETNAMESE_LOCALE, {
    timeZone: VIETNAM_TIMEZONE,
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
};

export const formatVietnamDate = (value, options = {}) => {
  const date = parseDate(value);
  if (!date) return '';

  return new Intl.DateTimeFormat(VIETNAMESE_LOCALE, {
    timeZone: VIETNAM_TIMEZONE,
    ...options,
  }).format(date);
};

export const formatVietnamRelativeDate = (value) => {
  const dateKey = getDateKey(value);
  if (!dateKey) return '';

  const now = new Date();
  const yesterday = new Date();
  yesterday.setDate(now.getDate() - 1);

  if (dateKey === getDateKey(now)) {
    return formatVietnamTime(value);
  }

  if (dateKey === getDateKey(yesterday)) {
    return 'Hôm qua';
  }

  return formatVietnamDate(value, {
    day: '2-digit',
    month: '2-digit',
  });
};
