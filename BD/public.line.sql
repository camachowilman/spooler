
CREATE TABLE public.line (
	id int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	document_reference varchar(20) NOT NULL,
	document_type varchar(1) NOT NULL,
	document_number varchar(20) NULL,
	document_json text NULL,
	printer_serial varchar(20) NOT NULL,
	printed_note varchar(100) NULL,
	printed_date varchar(20) NULL,
	printed bool DEFAULT false NOT NULL,
	created_date timestamp NULL,
	reprint bool DEFAULT false NULL,
	CONSTRAINT line_pkey PRIMARY KEY (id)
);